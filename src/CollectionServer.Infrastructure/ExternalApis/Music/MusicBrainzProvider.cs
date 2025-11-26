using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CollectionServer.Infrastructure.ExternalApis.Music;

/// <summary>
/// MusicBrainz API 제공자
/// </summary>
public class MusicBrainzProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MusicBrainzSettings _settings;
    private readonly ILogger<MusicBrainzProvider> _logger;

    public MusicBrainzProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<MusicBrainzProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.MusicBrainz;
        _logger = logger;
    }

    public string ProviderName => "MusicBrainz";
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
            var httpClient = _httpClientFactory.CreateClient("MusicBrainz");
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("User-Agent", _settings.UserAgent);

            _logger.LogInformation("Querying MusicBrainz API for barcode: {Barcode}", barcode);

            var url = $"/ws/2/release/?query=barcode:{barcode}&fmt=json";
            
            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MusicBrainz API returned {StatusCode} for barcode: {Barcode}",
                    response.StatusCode, barcode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MusicBrainzSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (result?.Releases == null || result.Releases.Length == 0)
            {
                _logger.LogInformation("No results found from MusicBrainz for barcode: {Barcode}", barcode);
                return null;
            }

            var release = result.Releases[0];
            
            // Get full release details
            var detailsUrl = $"/ws/2/release/{release.Id}?inc=artists+recordings&fmt=json";
            var detailsResponse = await httpClient.GetAsync(detailsUrl, cancellationToken);
            
            if (!detailsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get release details from MusicBrainz for ID: {Id}", release.Id);
            }

            var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
            var details = JsonSerializer.Deserialize<MusicBrainzRelease>(detailsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var tracks = details?.Media?
                .SelectMany(m => m.Tracks ?? Array.Empty<MusicBrainzTrack>())
                .Select(t => new Track
                {
                    TrackNumber = t.Position ?? 0,
                    Title = t.Title ?? "Unknown",
                    DurationSeconds = t.Length.HasValue ? t.Length.Value / 1000 : 0
                })
                .ToList() ?? new List<Track>();

            var album = new MusicAlbum
            {
                Id = Guid.NewGuid(),
                Barcode = barcode,
                MediaType = MediaType.MusicAlbum,
                Title = release.Title ?? "Unknown",
                Description = $"Album by {release.ArtistCredit?[0].Artist?.Name ?? "Unknown Artist"}",
                ImageUrl = null, // MusicBrainz doesn't provide cover art directly
                Source = ProviderName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Artist = release.ArtistCredit?[0].Artist?.Name ?? "Unknown Artist",
                Tracks = tracks,
                ReleaseDate = ParseReleaseDate(release.Date),
                Label = release.LabelInfo?[0].Label?.Name,
                Genre = null // MusicBrainz doesn't provide genre in basic search
            };

            _logger.LogInformation("Successfully retrieved album from MusicBrainz: {Title}", album.Title);
            return album;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to MusicBrainz API timed out for barcode: {Barcode}", barcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying MusicBrainz API for barcode: {Barcode}", barcode);
            return null;
        }
    }

    private static DateTime? ParseReleaseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var date) ? date : null;
    }

    // Response DTOs
    private class MusicBrainzSearchResponse
    {
        public MusicBrainzReleaseSummary[]? Releases { get; set; }
        public int Count { get; set; }
    }

    private class MusicBrainzReleaseSummary
    {
        public string Id { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Date { get; set; }
        public string? Barcode { get; set; }
        public ArtistCredit[]? ArtistCredit { get; set; }
        public LabelInfo[]? LabelInfo { get; set; }
    }

    private class MusicBrainzRelease
    {
        public string Id { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Date { get; set; }
        public string? Barcode { get; set; }
        public ArtistCredit[]? ArtistCredit { get; set; }
        public LabelInfo[]? LabelInfo { get; set; }
        public Medium[]? Media { get; set; }
    }

    private class ArtistCredit
    {
        public Artist? Artist { get; set; }
    }

    private class Artist
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
    }

    private class LabelInfo
    {
        public Label? Label { get; set; }
    }

    private class Label
    {
        public string? Name { get; set; }
    }

    private class Medium
    {
        public MusicBrainzTrack[]? Tracks { get; set; }
    }

    private class MusicBrainzTrack
    {
        public int? Position { get; set; }
        public string? Title { get; set; }
        public int? Length { get; set; } // in milliseconds
    }
}
