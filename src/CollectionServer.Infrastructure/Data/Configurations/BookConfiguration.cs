using CollectionServer.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionServer.Infrastructure.Data.Configurations;

/// <summary>
/// Book 엔티티 구성
/// </summary>
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Isbn13)
            .HasMaxLength(13);

        builder.Property(b => b.Authors)
            .HasMaxLength(1000);

        builder.Property(b => b.Publisher)
            .HasMaxLength(200);

        builder.Property(b => b.Genre)
            .HasMaxLength(200);
    }
}
