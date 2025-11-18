using CollectionServer.Core.Entities;

namespace CollectionServer.Core.Interfaces;

/// <summary>
/// 미디어 서비스 인터페이스 (비즈니스 로직)
/// Database-First 아키텍처 구현
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// 바코드로 미디어 정보 조회
    /// 1. 데이터베이스 조회
    /// 2. 없으면 외부 API 호출 (우선순위 기반 폴백)
    /// 3. 외부 API 결과를 데이터베이스에 저장
    /// </summary>
    Task<MediaItem> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID로 미디어 정보 조회
    /// </summary>
    Task<MediaItem?> GetMediaByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
