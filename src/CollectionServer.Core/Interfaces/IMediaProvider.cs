using CollectionServer.Core.Entities;

namespace CollectionServer.Core.Interfaces;

/// <summary>
/// 외부 미디어 데이터 제공자 인터페이스
/// (Google Books, TMDb, MusicBrainz 등)
/// </summary>
public interface IMediaProvider
{
    /// <summary>
    /// 제공자 이름 (예: "GoogleBooks", "TMDb")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 우선순위 (낮을수록 우선, 1 = 최우선)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 이 제공자가 지원하는 바코드 형식인지 확인
    /// </summary>
    bool SupportsBarcode(string barcode);

    /// <summary>
    /// 바코드로 미디어 정보 조회
    /// </summary>
    Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}
