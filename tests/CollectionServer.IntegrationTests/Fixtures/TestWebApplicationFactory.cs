using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Data;
using CollectionServer.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CollectionServer.IntegrationTests.Fixtures;

/// <summary>
/// 통합 테스트용 WebApplicationFactory
/// In-Memory 데이터베이스를 사용하여 실제 HTTP 요청을 테스트
/// User Secrets에서 API 키를 로드하여 외부 API 통합 테스트 수행
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // User Secrets 추가 (API 키)
            config.AddUserSecrets<Program>();
        });

        builder.ConfigureServices(services =>
        {
            // 기존 DbContext 등록 제거
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 기존 ICacheService 및 IConnectionMultiplexer 제거
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);

            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null) services.Remove(redisDescriptor);

            // FakeCacheService 등록
            services.AddSingleton<ICacheService, FakeCacheService>();

            // Mock HttpMessageHandler 등록
            services.AddTransient<MockHttpMessageHandler>();

            // 모든 외부 API Client가 MockHandler를 사용하도록 설정
            var providerNames = new[] 
            { 
                "GoogleBooks", "KakaoBook", "AladinApi", 
                "TMDb", "OMDb", "UpcItemDb", 
                "MusicBrainz", "Discogs" 
            };

            foreach (var name in providerNames)
            {
                services.AddHttpClient(name)
                    .ConfigurePrimaryHttpMessageHandler<MockHttpMessageHandler>();
            }

            // In-Memory 데이터베이스로 교체 (테스트 클래스별 격리)
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // 데이터베이스 초기화
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// 테스트 간 데이터베이스 및 캐시 초기화
    /// </summary>
    public void ResetDatabase()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // 캐시 초기화
            var cache = Services.GetRequiredService<ICacheService>() as FakeCacheService;
            cache?.Clear();
        }
        catch
        {
            // 테스트 정리 과정에서의 오류는 무시하여 테스트 실행기 충돌 방지
        }
    }
}
