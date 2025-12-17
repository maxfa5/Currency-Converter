using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Converter
{
    public class CurrencyConverterViewModel : INotifyPropertyChanged
    {
        private readonly ICurrencyService _currencyService;
        private readonly ICacheService _cacheService;
        private DateTime _selectedDate;
        private List<Currency> _currencies;
        private Currency _selectedFromCurrency;
        private Currency _selectedToCurrency;
        private decimal _amount;
        private decimal _convertedAmount;
        private bool _isLoading;
        private string _statusMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value.Date)
                {
                    _selectedDate = value.Date;
                    OnPropertyChanged();

                    _ = LoadCurrenciesAsync();
                }
            }
        }

        public DateTime MaxDate => DateTime.Today;
        public DateTime MinDate => new DateTime(1991, 1, 1);

        public List<Currency> Currencies
        {
            get => _currencies;
            set
            {
                _currencies = value;
                OnPropertyChanged();
            }
        }

        public Currency SelectedFromCurrency
        {
            get => _selectedFromCurrency;
            set
            {
                if (_selectedFromCurrency != value)
                {
                    _selectedFromCurrency = value;
                    OnPropertyChanged();
                    ConvertCurrency();
                }
            }
        }

        public Currency SelectedToCurrency
        {
            get => _selectedToCurrency;
            set
            {
                if (_selectedToCurrency != value)
                {
                    _selectedToCurrency = value;
                    OnPropertyChanged();
                    ConvertCurrency();
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    OnPropertyChanged();
                    ConvertCurrency();
                }
            }
        }

        public decimal ConvertedAmount
        {
            get => _convertedAmount;
            set
            {
                _convertedAmount = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadDataCommand { get; }
        public ICommand SwapCurrenciesCommand { get; }
        public ICommand ClearCacheCommand { get; }

        public CurrencyConverterViewModel(ICurrencyService currencyService, ICacheService cacheService)
        {
            _currencyService = currencyService;
            _cacheService = cacheService;

            _selectedDate = DateTime.Today;
            _amount = 100;

            LoadDataCommand = new Command(async () => await LoadCurrenciesAsync());
            SwapCurrenciesCommand = new Command(SwapCurrencies);
            ClearCacheCommand = new Command(ClearCache);

            _ = LoadCurrenciesAsync();
        }

        private async Task LoadCurrenciesAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Загрузка курсов на {SelectedDate:dd.MM.yyyy}...";

                var response = await _currencyService.GetExchangeRatesAsync(SelectedDate);

                if (response.Date.Date != SelectedDate.Date)
                {
                    StatusMessage = $"Данные за {SelectedDate:dd.MM.yyyy} не найдены. Показаны курсы на {response.Date:dd.MM.yyyy}";
                    SelectedDate = response.Date.Date;
                }
                else
                {
                    StatusMessage = $"Курсы на {response.Date:dd.MM.yyyy}";
                }

                Currencies = _currencyService.GetCurrenciesFromResponse(response);

                if (SelectedFromCurrency == null)
                {
                    SelectedFromCurrency = Currencies.FirstOrDefault(c => c.CharCode == "USD")
                                         ?? Currencies.FirstOrDefault();
                }

                if (SelectedToCurrency == null)
                {
                    SelectedToCurrency = Currencies.FirstOrDefault(c => c.CharCode == "RUB")
                                       ?? Currencies.FirstOrDefault();
                }

                ConvertCurrency();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка. Не удалось загрузить курсы валют: {ex.Message}";
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ConvertCurrency()
        {
            if (SelectedFromCurrency == null || SelectedToCurrency == null || Amount <= 0)
                return;

            try
            {
                ConvertedAmount = _currencyService.ConvertWithRates(
                    Amount,
                    SelectedFromCurrency,
                    SelectedToCurrency);
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка конвертации";
                ConvertedAmount = 0;
                Console.WriteLine($"Ошибка конвертации: {ex.Message}");
            }
        }

        private void SwapCurrencies()
        {
            (SelectedFromCurrency, SelectedToCurrency) = (SelectedToCurrency, SelectedFromCurrency);
        }

        private void ClearCache()
        {
            _cacheService.ClearCache();
            StatusMessage = "Кеш очищен";

            _ = LoadCurrenciesAsync();
        }

        

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}