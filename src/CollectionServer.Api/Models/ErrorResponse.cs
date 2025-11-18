namespace CollectionServer.Api.Models;

/// <summary>
/// 오류 응답 DTO
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP 상태 코드
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 오류 메시지 (한국어)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 상세 오류 정보
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 오류 발생 시각 (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 추적 ID
    /// </summary>
    public string? TraceId { get; set; }
}
