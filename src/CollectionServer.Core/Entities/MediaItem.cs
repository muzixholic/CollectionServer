using CollectionServer.Core.Enums;

namespace CollectionServer.Core.Entities;

/// <summary>
/// 모든 미디어 항목의 추상 기본 클래스
/// TPT (Table Per Type) 전략 사용
/// </summary>
public abstract class MediaItem
{
    /// <summary>
    /// 기본 키 (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 바코드 (ISBN-10/13, UPC, EAN-13)
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 미디어 유형 (Book, Movie, MusicAlbum)
    /// </summary>
    public MediaType MediaType { get; set; }

    /// <summary>
    /// 미디어 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 설명/줄거리/소개
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 표지/커버 이미지 URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 데이터 출처 (GoogleBooks, TMDb 등)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 생성 시각 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 최종 수정 시각 (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
