using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Services;
using CollectionServer.Infrastructure.Data;
using CollectionServer.Infrastructure.ExternalApis.Books;
using CollectionServer.Infrastructure.ExternalApis.Movies;
using CollectionServer.Infrastructure.ExternalApis.Music;
using CollectionServer.Infrastructure.Options;
using CollectionServer.Infrastructure.Repositories;
using CollectionServer.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using StackExchange.Redis;
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
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });
        });

        return services;
    }
}
