using CollectionServer.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// 테스트 간 데이터베이스 초기화
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}
