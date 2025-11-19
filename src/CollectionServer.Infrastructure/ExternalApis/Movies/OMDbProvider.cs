using CollectionServer.Core.Entities;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// OMDb (Open Movie Database) API 제공자
/// </summary>
public class OMDbProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OMDbSettings _settings;
    private readonly ILogger<OMDbProvider> _logger;

    public OMDbProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<OMDbProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.OMDb;
        _logger = logger;
    }

    public string ProviderName => "OMDb";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        // OMDb: UPC only (12 digits), not ISBN
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        return cleaned.Length == 12 && cleaned.All(char.IsDigit);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // TODO: Implement OMDb API integration
        _logger.LogInformation("OMDbProvider - Querying for barcode: {Barcode}", barcode);
        await Task.CompletedTask;
        return null;
    }
}
