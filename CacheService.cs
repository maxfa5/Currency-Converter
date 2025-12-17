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

    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime Expiration { get; set; }
            public bool IsExpired => DateTime.Now > Expiration;
        }

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
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

        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}