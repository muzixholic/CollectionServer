using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Services;
using CollectionServer.Infrastructure.Data;
using CollectionServer.Infrastructure.ExternalApis.Books;
using CollectionServer.Infrastructure.ExternalApis.Movies;
using CollectionServer.Infrastructure.ExternalApis.Music;
using CollectionServer.Infrastructure.Options;
using CollectionServer.Infrastructure.Repositories;
using CollectionServer.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Primitives;
using Polly;
using StackExchange.Redis;
using System.Globalization;
using System.Threading.RateLimiting;

namespace CollectionServer.Api.Extensions;

/// <summary>
/// 의존성 주입 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 데이터베이스 서비스 등록
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.MaxBatchSize(100);
            });
            
            // 성능 최적화: 쿼리 캐싱 및 연결 풀링
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<BarcodeValidator>();

        // Cache 등록 (Garnet)
        var cacheConnection = configuration.GetConnectionString("CacheConnection") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(cacheConnection));
        services.AddSingleton<ICacheService, GarnetCacheService>();

        return services;
    }

    /// <summary>
    /// 외부 API 설정 및 제공자 등록
    /// </summary>
    public static IServiceCollection AddExternalApiSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalApiSettings>(configuration.GetSection("ExternalApis"));

        // HttpClient factory 구성
        services.AddHttpClient();

        services.AddScoped<IUpcResolver, UpcItemDbResolver>();

        // Resilience Pipeline 설정
        var resilienceOptions = new HttpStandardResilienceOptions
        {
            Retry = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
            },
            CircuitBreaker = new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 10
            },
            TotalRequestTimeout = new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            }
        };

        // Book providers
        services.AddHttpClient("GoogleBooks")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, GoogleBooksProvider>();

        services.AddHttpClient("KakaoBook")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, KakaoBookProvider>();

        services.AddHttpClient("AladinApi")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, AladinApiProvider>();

        // Movie providers
        services.AddHttpClient("TMDb")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, TMDbProvider>();

        services.AddHttpClient("OMDb")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, OMDbProvider>();

        services.AddHttpClient("UpcItemDb")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });

        // Music providers
        services.AddHttpClient("MusicBrainz")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, MusicBrainzProvider>();

        services.AddHttpClient("Discogs")
            .AddStandardResilienceHandler(options => 
            {
                options.Retry = resilienceOptions.Retry;
                options.CircuitBreaker = resilienceOptions.CircuitBreaker;
                options.TotalRequestTimeout = resilienceOptions.TotalRequestTimeout;
            });
        services.AddScoped<IMediaProvider, DiscogsProvider>();

        return services;
    }

    /// <summary>
    /// Rate Limiting 설정
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSection = configuration.GetSection("RateLimiting");
        var permitLimit = rateLimitSection.GetValue<int?>("PermitLimit") ?? 100;
        var windowSeconds = rateLimitSection.GetValue<int?>("WindowSeconds") ?? 60;
        var queueLimit = rateLimitSection.GetValue<int?>("QueueLimit") ?? 10;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = TimeSpan.FromSeconds(windowSeconds);
                if (context.Lease?.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan metadataRetryAfter) == true)
                {
                    retryAfter = metadataRetryAfter;
                }

                var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds();

                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                response.Headers["X-RateLimit-Limit"] = permitLimit.ToString(CultureInfo.InvariantCulture);
                response.Headers["X-RateLimit-Remaining"] = "0";
                response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString(CultureInfo.InvariantCulture);

                var payload = new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    message = "요청 제한을 초과했습니다. 잠시 후 다시 시도해주세요.",
                    retryAfterSeconds,
                    limit = permitLimit,
                    remaining = 0
                };

                response.ContentType = "application/json";
                await response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                });
            });
        });

        return services;
    }

    private static string ResolveClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwarded) && !StringValues.IsNullOrEmpty(forwarded))
        {
            var first = forwarded.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
