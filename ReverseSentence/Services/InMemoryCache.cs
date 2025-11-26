using Microsoft.Extensions.Caching.Memory;

namespace ReverseSentence.Services
{
    public class InMemoryCache : ICache
    {
        private readonly IMemoryCache cache;
        private readonly ILogger<InMemoryCache> logger;

        public InMemoryCache(IMemoryCache cache, ILogger<InMemoryCache> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (cache.TryGetValue(key, out T? value))
            {
                logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetSlidingExpiration(expiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromHours(1)); // Default 1 hour sliding
            }

            cache.Set(key, value, options);
            logger.LogDebug("Cached value for key: {Key} with sliding expiration: {Expiration}", 
                key, expiration ?? TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            cache.Remove(key);
            logger.LogDebug("Removed cache for key: {Key}", key);
            return Task.CompletedTask;
        }
    }
}
