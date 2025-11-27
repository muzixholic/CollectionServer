namespace CollectionServer.Core.Models;

/// <summary>
/// 바코드 해석 결과 모델
/// </summary>
public class UpcResolutionResult
{
    public string Barcode { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? CleanTitle { get; init; }
    public int? ReleaseYear { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int? TmdbId { get; init; }
    public string? ImdbId { get; init; }
}
