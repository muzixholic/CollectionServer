using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Services;
using CollectionServer.Infrastructure.Data;
using CollectionServer.Infrastructure.Options;
using CollectionServer.Infrastructure.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
            });
        });

        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<BarcodeValidator>();

        return services;
    }

    /// <summary>
    /// 외부 API 설정 등록
    /// </summary>
    public static IServiceCollection AddExternalApiSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalApiSettings>(configuration.GetSection("ExternalApis"));
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
