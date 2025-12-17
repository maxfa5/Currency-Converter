using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Net.WebRequestMethods;

namespace Converter
{
    public interface ICurrencyService
    {
        Task<CbrApiResponse> GetExchangeRatesAsync(DateTime date);
        Task<List<Currency>> GetAvailableCurrenciesAsync(DateTime date);
        List<Currency> GetCurrenciesFromResponse(CbrApiResponse response);
        decimal ConvertWithRates(decimal amount, Currency fromCurrency, Currency toCurrency);
    }
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly JsonSerializerOptions _jsonOptions;


        public CurrencyService(ICacheService cacheService)
    {
        _cacheService = cacheService;
        
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CurrencyConverter/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

        public async Task<CbrApiResponse> GetExchangeRatesAsync(DateTime date)
        {
            // Используем кеширование через CacheService
            return await _cacheService.GetOrCreateAsync(
                $"exchange_rates_{date:yyyyMMdd}",
                async () =>
                {
                    try
                    {
                        string url;

                        if (date.Date == DateTime.Today.Date)
                        {
                            url = "https://www.cbr-xml-daily.ru/daily_json.js";
                        }
                        else
                        {
                            url = $"https://www.cbr-xml-daily.ru/archive/{date.Year}/{date.Month:D2}/{date.Day:D2}/daily_json.js";
                        }

                        Console.WriteLine($"Запрос к API: {url}");

                        var response = await _httpClient.GetStringAsync(url);

                        if (string.IsNullOrWhiteSpace(response))
                            throw new Exception("Получен пустой ответ от сервера");

                        return JsonSerializer.Deserialize<CbrApiResponse>(response, _jsonOptions);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return await GetNearestAvailableDate(date);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Ошибка: {ex.Message}");
                    }
                },
                date.Date == DateTime.Today ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1)
            );
        }

        private async Task<CbrApiResponse> GetNearestAvailableDate(DateTime date)
        {
            for (int i = 1; i <= 14; i++)
            {
                var previousDate = date.AddDays(-i);
                try
                {
                    return await GetExchangeRatesAsync(previousDate);
                }
                catch
                {
                    continue;
                }
            }
            throw new Exception("Не удалось найти данные за ближайшие 14 дней");
        }


        public async Task<List<Currency>> GetAvailableCurrenciesAsync(DateTime date)
            {
                var response = await GetExchangeRatesAsync(date);
                return GetCurrenciesFromResponse(response);
            }

            public List<Currency> GetCurrenciesFromResponse(CbrApiResponse response)
            {
                if (response?.Valute == null)
                    return new List<Currency>();

                var currencies = response.Valute.Values.ToList();

                // Добавляем RUB вручную
                currencies.Add(new Currency
                {
                    ID = "R00000",
                    NumCode = "643",
                    CharCode = "RUB",
                    Nominal = 1,
                    Name = "Российский рубль",
                    Value = 1,
                    Previous = 1
                });

                return currencies.OrderBy(c => c.CharCode).ToList();
            }

            public decimal ConvertWithRates(decimal amount, Currency fromCurrency, Currency toCurrency)
            {
                if (fromCurrency == null || toCurrency == null)
                    throw new ArgumentNullException("Валюты не могут быть null");

                // Все курсы в API относительно RUB
                decimal fromRate = fromCurrency.CharCode == "RUB" ? 1 : fromCurrency.Value / fromCurrency.Nominal;
                decimal toRate = toCurrency.CharCode == "RUB" ? 1 : toCurrency.Value / toCurrency.Nominal;

                // Конвертация: amount в fromCurrency → RUB → toCurrency
                decimal amountInRub = amount * fromRate;
                return amountInRub / toRate;
            }
        }
    }
