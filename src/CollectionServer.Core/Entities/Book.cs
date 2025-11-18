using CollectionServer.Core.Enums;

namespace CollectionServer.Core.Entities;

/// <summary>
/// 도서 엔티티
/// </summary>
public class Book : MediaItem
{
    public Book()
    {
        MediaType = MediaType.Book;
    }

    /// <summary>
    /// ISBN-13 (정규화된 13자리)
    /// </summary>
    public string? Isbn13 { get; set; }

    /// <summary>
    /// 저자 목록 (쉼표로 구분)
    /// </summary>
    public string? Authors { get; set; }

    /// <summary>
    /// 출판사
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// 출판 날짜
    /// </summary>
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// 페이지 수
    /// </summary>
    public int? PageCount { get; set; }

    /// <summary>
    /// 장르/카테고리
    /// </summary>
    public string? Genre { get; set; }
}
