using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionServer.Infrastructure.Data.Configurations;

/// <summary>
/// MediaItem 엔티티 구성 (TPH 전략)
/// </summary>
public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("MediaItems");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Barcode)
            .IsRequired()
            .HasMaxLength(13);

        builder.HasIndex(m => m.Barcode)
            .IsUnique()
            .HasDatabaseName("idx_barcode");

        builder.HasIndex(m => m.MediaType)
            .HasDatabaseName("idx_media_type");

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Description)
            .HasMaxLength(5000);

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(2000);

        builder.Property(m => m.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        // TPH (Table Per Hierarchy) 상속 전략
        // MediaType enum을 Discriminator로 사용
        builder.HasDiscriminator(m => m.MediaType)
            .HasValue<Book>(MediaType.Book)
            .HasValue<Movie>(MediaType.Movie)
            .HasValue<MusicAlbum>(MediaType.MusicAlbum);
    }
}
