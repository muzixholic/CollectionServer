namespace CollectionServer.Core.Interfaces;

/// <summary>
/// 분산 캐시 서비스 인터페이스
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 캐시에서 항목 조회
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시에 항목 저장
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시에서 항목 삭제
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}