using CollectionServer.Core.Enums;

namespace CollectionServer.Core.Entities;

/// <summary>
/// 영화 엔티티 (Blu-ray, DVD 포함)
/// </summary>
public class Movie : MediaItem
{
    public Movie()
    {
        MediaType = MediaType.Movie;
    }

    /// <summary>
    /// 감독
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// 출연진 (쉼표로 구분)
    /// </summary>
    public string? Cast { get; set; }

    /// <summary>
    /// 상영 시간 (분)
    /// </summary>
    public int? RuntimeMinutes { get; set; }

    /// <summary>
    /// 개봉 날짜
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// 등급 (예: 12세 관람가, 15세 관람가)
    /// </summary>
    public string? Rating { get; set; }

    /// <summary>
    /// 장르 (쉼표로 구분)
    /// </summary>
    public string? Genre { get; set; }
}
