using System.Text.Json;
using CollectionServer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CollectionServer.Infrastructure.Services;

/// <summary>
/// Garnet (Redis 호환) 기반 캐시 서비스 구현
/// </summary>
public class GarnetCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<GarnetCacheService> _logger;
    private readonly IDatabase _database;

    public GarnetCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<GarnetCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _database = _connectionMultiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key {Key} from cache", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiration, When.Always, CommandFlags.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in cache", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from cache", key);
        }
    }
}