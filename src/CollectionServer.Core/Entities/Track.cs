namespace CollectionServer.Core.Entities;

/// <summary>
/// 음악 트랙 값 객체
/// </summary>
public class Track
{
    /// <summary>
    /// 트랙 번호
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// 트랙 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 트랙 길이 (초)
    /// </summary>
    public int? DurationSeconds { get; set; }
}
