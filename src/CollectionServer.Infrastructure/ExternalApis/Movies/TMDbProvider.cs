using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// TMDb (The Movie Database) API 제공자
/// </summary>
public class TMDbProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TMDbSettings _settings;
    private readonly ILogger<TMDbProvider> _logger;

    public TMDbProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<TMDbProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.TMDb;
        _logger = logger;
    }

    public string ProviderName => "TMDb";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        // UPC (12 digits) or EAN-13 (13 digits, not ISBN)
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        if (cleaned.Length == 12) return true;
        if (cleaned.Length == 13 && !cleaned.StartsWith("978") && !cleaned.StartsWith("979")) return true;
        return false;
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("TMDb");
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            // TMDb doesn't directly support UPC search, so we need to search by external ID
            // This is a simplified implementation - in production, you'd need more sophisticated logic
            _logger.LogInformation("Querying TMDb API for barcode: {Barcode}", barcode);

            // For MVP, we'll search by title match as TMDb doesn't have native barcode support
            // In production, you'd integrate with a barcode-to-TMDb ID mapping service
            _logger.LogWarning("TMDb API does not natively support barcode lookup. Barcode: {Barcode}", barcode);
            return null;

            // Placeholder for future implementation with proper barcode mapping
            // var url = $"/search/movie?api_key={_settings.ApiKey}&query={barcode}";
            // ... rest of implementation
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to TMDb API timed out for barcode: {Barcode}", barcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying TMDb API for barcode: {Barcode}", barcode);
            return null;
        }
    }

    private static DateTime? ParseReleaseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var date) ? date : null;
    }

    // Response DTOs
    private class TMDbSearchResponse
    {
        public int Page { get; set; }
        public TMDbMovie[]? Results { get; set; }
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
    }

    private class TMDbMovie
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? ReleaseDate { get; set; }
        public decimal VoteAverage { get; set; }
    }

    private class TMDbMovieDetails
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public TMDbGenre[]? Genres { get; set; }
        public TMDbCredits? Credits { get; set; }
    }

    private class TMDbGenre
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class TMDbCredits
    {
        public TMDbCastMember[]? Cast { get; set; }
        public TMDbCrewMember[]? Crew { get; set; }
    }

    private class TMDbCastMember
    {
        public string? Name { get; set; }
        public string? Character { get; set; }
    }

    private class TMDbCrewMember
    {
        public string? Name { get; set; }
        public string? Job { get; set; }
        public string? Department { get; set; }
    }
}
