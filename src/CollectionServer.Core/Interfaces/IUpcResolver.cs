using CollectionServer.Core.Models;

namespace CollectionServer.Core.Interfaces;

/// <summary>
/// UPC/Barcode 메타데이터 해석기
/// </summary>
public interface IUpcResolver
{
    /// <summary>
    /// UPC / EAN-13 바코드를 기반으로 제목, 연도, TMDb/IMDb ID 등을 해석한다.
    /// </summary>
    Task<UpcResolutionResult?> ResolveAsync(string barcode, CancellationToken cancellationToken = default);
}
