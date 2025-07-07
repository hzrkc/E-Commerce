using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var cachedValue = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(cachedValue))
            return null;

        return JsonSerializer.Deserialize<T>(cachedValue);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
            options.SetSlidingExpiration(expiration.Value);

        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedValue, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // Redis'te pattern-based removal için özel implementation gerekiyor
        // Şimdilik basit bir implementasyon
        // Gerçek projede IConnectionMultiplexer kullanılabilir
        await Task.CompletedTask;
    }
}