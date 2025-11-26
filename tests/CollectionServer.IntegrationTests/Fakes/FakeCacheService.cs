using CollectionServer.Core.Interfaces;
using System.Collections.Concurrent;

namespace CollectionServer.IntegrationTests.Fakes;

/// <summary>
/// 테스트용 인메모리 캐시 서비스
/// </summary>
public class FakeCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (value != null)
        {
            _cache[key] = value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _cache.Clear();
    }
}