using CollectionServer.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionServer.Infrastructure.Data.Configurations;

/// <summary>
/// Movie 엔티티 구성
/// </summary>
public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(m => m.Director)
            .HasMaxLength(200);

        builder.Property(m => m.Cast)
            .HasMaxLength(2000);

        builder.Property(m => m.Rating)
            .HasMaxLength(50);

        builder.Property(m => m.Genre)
            .HasMaxLength(200);
    }
}
