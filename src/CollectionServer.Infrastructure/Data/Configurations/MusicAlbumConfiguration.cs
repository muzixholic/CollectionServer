using CollectionServer.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionServer.Infrastructure.Data.Configurations;

/// <summary>
/// MusicAlbum 엔티티 구성
/// </summary>
public class MusicAlbumConfiguration : IEntityTypeConfiguration<MusicAlbum>
{
    public void Configure(EntityTypeBuilder<MusicAlbum> builder)
    {
        builder.Property(m => m.Artist)
            .HasMaxLength(200);

        builder.Property(m => m.Label)
            .HasMaxLength(200);

        builder.Property(m => m.Genre)
            .HasMaxLength(200);

        // Tracks를 JSON으로 저장
        builder.OwnsMany(m => m.Tracks, track =>
        {
            track.ToJson();
            track.Property(t => t.Title).HasMaxLength(500);
        });
    }
}
