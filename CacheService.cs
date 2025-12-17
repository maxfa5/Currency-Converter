using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Converter
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null);
        void ClearCache();
    }
    public class CacheItem
    {
        public object Value { get; set; }
        public DateTime Expiration { get; set; }
        public bool IsExpired => DateTime.Now > Expiration;
    }
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private DateTime _lastCleanup = DateTime.Now;

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            AutoCleanup();

            var defaultExpiration = TimeSpan.FromHours(1);
            var actualExpiration = expiration ?? defaultExpiration;

            if (_cache.TryGetValue(cacheKey, out var cacheItem) && !cacheItem.IsExpired)
            {
                return (T)cacheItem.Value;
            }

            var data = await factory();

            _cache[cacheKey] = new CacheItem
            {
                Value = data,
                Expiration = DateTime.Now.Add(actualExpiration)
            };

            return data;
        }

        private void AutoCleanup()
        {
            if (DateTime.Now - _lastCleanup < _cleanupInterval)
                return;

            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            _lastCleanup = DateTime.Now;

            Console.WriteLine($"Очистка кеша: удалено {expiredKeys.Count} устаревших записей");
        }

        public void ClearCache()
        {
            _cache.Clear();
            _lastCleanup = DateTime.Now;
        }
    }
}