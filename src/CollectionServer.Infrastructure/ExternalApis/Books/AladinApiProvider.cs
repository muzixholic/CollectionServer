using CollectionServer.Core.Entities;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectionServer.Infrastructure.ExternalApis.Books;

/// <summary>
/// Aladin API 제공자
/// </summary>
public class AladinApiProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AladinApiSettings _settings;
    private readonly ILogger<AladinApiProvider> _logger;

    public AladinApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<AladinApiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.AladinApi;
        _logger = logger;
    }

    public string ProviderName => "AladinApi";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        return (cleaned.Length == 10 || cleaned.Length == 13) &&
               (cleaned.StartsWith("978") || cleaned.StartsWith("979") || cleaned.Length == 10);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // TODO: Implement Aladin API integration
        _logger.LogInformation("AladinApiProvider - Querying for barcode: {Barcode}", barcode);
        await Task.CompletedTask;
        return null;
    }
}
