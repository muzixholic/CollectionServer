using CollectionServer.Core.Entities;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectionServer.Infrastructure.ExternalApis.Books;

/// <summary>
/// Kakao Book Search API 제공자
/// </summary>
public class KakaoBookProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KakaoBookSettings _settings;
    private readonly ILogger<KakaoBookProvider> _logger;

    public KakaoBookProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<KakaoBookProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.KakaoBook;
        _logger = logger;
    }

    public string ProviderName => "KakaoBook";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        return (cleaned.Length == 10 || cleaned.Length == 13) &&
               (cleaned.StartsWith("978") || cleaned.StartsWith("979") || cleaned.Length == 10);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // TODO: Implement Kakao Book Search API integration
        _logger.LogInformation("KakaoBookProvider - Querying for barcode: {Barcode}", barcode);
        await Task.CompletedTask;
        return null;
    }
}
