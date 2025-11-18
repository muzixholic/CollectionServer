using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;

namespace CollectionServer.Core.Interfaces;

/// <summary>
/// 미디어 리포지토리 인터페이스
/// </summary>
public interface IMediaRepository
{
    /// <summary>
    /// 바코드로 미디어 항목 조회
    /// </summary>
    Task<MediaItem?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID로 미디어 항목 조회
    /// </summary>
    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 미디어 항목 추가
    /// </summary>
    Task<MediaItem> AddAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 미디어 항목 업데이트
    /// </summary>
    Task UpdateAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 미디어 항목 삭제
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 미디어 타입별 목록 조회
    /// </summary>
    Task<List<MediaItem>> GetByMediaTypeAsync(MediaType mediaType, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}
