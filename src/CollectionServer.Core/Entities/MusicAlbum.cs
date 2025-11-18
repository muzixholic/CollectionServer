using CollectionServer.Core.Enums;

namespace CollectionServer.Core.Entities;

/// <summary>
/// 음악 앨범 엔티티
/// </summary>
public class MusicAlbum : MediaItem
{
    public MusicAlbum()
    {
        MediaType = MediaType.MusicAlbum;
        Tracks = new List<Track>();
    }

    /// <summary>
    /// 아티스트/밴드명
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// 트랙 목록
    /// </summary>
    public List<Track> Tracks { get; set; }

    /// <summary>
    /// 발매 날짜
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// 음반사/레이블
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 장르
    /// </summary>
    public string? Genre { get; set; }
}
