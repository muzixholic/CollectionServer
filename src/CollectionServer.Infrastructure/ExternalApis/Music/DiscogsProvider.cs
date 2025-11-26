using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectionServer.Infrastructure.ExternalApis.Music;

public class DiscogsProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiscogsSettings _settings;
    private readonly ILogger<DiscogsProvider> _logger;

    public DiscogsProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<DiscogsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.Discogs;
        _logger = logger;
    }

    public string ProviderName => "Discogs";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        
        // UPC: 12 digits, not starting with 978/979
        if (cleaned.Length == 12 && !cleaned.StartsWith("978") && !cleaned.StartsWith("979")) 
            return true;
        
        // EAN-13: 13 digits, not ISBN
        if (cleaned.Length == 13 && !cleaned.StartsWith("978") && !cleaned.StartsWith("979")) 
            return true;
        
        return false;
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("Discogs");
        _logger.LogInformation("Discogs Provider - barcode search not yet implemented: {Barcode}", barcode);
        await Task.CompletedTask;
        return null;
    }
}
