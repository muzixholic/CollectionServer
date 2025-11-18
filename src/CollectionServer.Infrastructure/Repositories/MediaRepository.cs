using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollectionServer.Infrastructure.Repositories;

/// <summary>
/// 미디어 리포지토리 구현
/// </summary>
public class MediaRepository : IMediaRepository
{
    private readonly ApplicationDbContext _context;

    public MediaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaItem?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Barcode == barcode, cancellationToken);
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<MediaItem> AddAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        mediaItem.Id = Guid.NewGuid();
        await _context.MediaItems.AddAsync(mediaItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return mediaItem;
    }

    public async Task UpdateAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        _context.MediaItems.Update(mediaItem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _context.MediaItems.FindAsync(new object[] { id }, cancellationToken);
        if (mediaItem != null)
        {
            _context.MediaItems.Remove(mediaItem);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<MediaItem>> GetByMediaTypeAsync(MediaType mediaType, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .AsNoTracking()
            .Where(m => m.MediaType == mediaType)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
