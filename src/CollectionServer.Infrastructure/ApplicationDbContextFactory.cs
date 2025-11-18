using CollectionServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CollectionServer.Infrastructure;

/// <summary>
/// EF Core 디자인 타임 DbContext Factory
/// dotnet ef migrations 명령 실행 시 사용됩니다.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // 디자인 타임용 기본 연결 문자열
        optionsBuilder.UseNpgsql("Host=localhost;Database=collectionserver_dev;Username=postgres;Password=postgres");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
