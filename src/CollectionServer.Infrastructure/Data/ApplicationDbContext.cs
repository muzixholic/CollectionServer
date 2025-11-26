using CollectionServer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CollectionServer.Infrastructure.Data;

/// <summary>
/// 애플리케이션 데이터베이스 컨텍스트
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fluent API 구성 적용
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // UpdatedAt 자동 업데이트
        var entries = ChangeTracker.Entries<MediaItem>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        // CreatedAt 자동 설정
        var addedEntries = ChangeTracker.Entries<MediaItem>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in addedEntries)
        {
            entry.Entity.CreatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
