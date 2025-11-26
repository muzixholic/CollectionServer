using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// UPCitemdb -> TMDb Bridge Provider
/// Uses UPCitemdb to get title from barcode, then TMDb for details
/// </summary>
public class UpcItemDbProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpcItemDbSettings _settings;
    private readonly TMDbSettings _tmdbSettings;
    private readonly ILogger<UpcItemDbProvider> _logger;

    public UpcItemDbProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<UpcItemDbProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.UpcItemDb;
        _tmdbSettings = settings.Value.TMDb;
        _logger = logger;
    }

    public string ProviderName => "UpcItemDb+TMDb";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        // UPC (12) or EAN (13)
        return cleaned.Length == 12 || (cleaned.Length == 13 && !cleaned.StartsWith("978") && !cleaned.StartsWith("979"));
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Get Title from UPCitemdb
            var title = await GetTitleFromUpcAsync(barcode, cancellationToken);
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }

            // Step 2: Search TMDb by Title
            var movieId = await SearchTmdbIdAsync(title, cancellationToken);
            if (!movieId.HasValue)
            {
                return null;
            }

            // Step 3: Get Details from TMDb
            return await GetTmdbDetailsAsync(movieId.Value, barcode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpcItemDbProvider for barcode: {Barcode}", barcode);
            return null;
        }
    }

    private async Task<string?> GetTitleFromUpcAsync(string barcode, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        var url = $"{_settings.BaseUrl}/lookup?upc={barcode}";
        _logger.LogInformation("Querying UPCitemdb: {Url}", url);

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UPCitemdb returned {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<UpcResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Items == null || result.Items.Length == 0)
        {
            return null;
        }

        var rawTitle = result.Items[0].Title;
        return CleanTitle(rawTitle);
    }

    private string CleanTitle(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "";
        
        // Remove common suffixes like [Blu-ray], (DVD), etc.
        var cleaned = title;
        string[] suffixes = { "[Blu-ray]", "[DVD]", "(Blu-ray)", "(DVD)", "Blu-ray", "DVD", "4K Ultra HD" };
        
        foreach (var suffix in suffixes)
        {
            cleaned = cleaned.Replace(suffix, "", StringComparison.OrdinalIgnoreCase);
        }

        // Remove text in brackets if it looks like format info
        // Simple heuristic: just take the part before first bracket if it exists
        // But be careful about titles like "Mission: Impossible (1996)"
        
        return cleaned.Trim();
    }

    private async Task<int?> SearchTmdbIdAsync(string title, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_tmdbSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_tmdbSettings.TimeoutSeconds);

        var url = $"/3/search/movie?api_key={_tmdbSettings.ApiKey}&query={HttpUtility.UrlEncode(title)}";
        
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Results == null || result.Results.Length == 0) return null;

        // Return first result
        return result.Results[0].Id;
    }

    private async Task<MediaItem?> GetTmdbDetailsAsync(int movieId, string barcode, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_tmdbSettings.BaseUrl);
        
        // Append credits to get cast/director
        var url = $"/3/movie/{movieId}?api_key={_tmdbSettings.ApiKey}&append_to_response=credits";

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var details = JsonSerializer.Deserialize<TmdbMovieDetails>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (details == null) return null;

        return new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.Movie,
            Title = details.Title,
            Description = details.Overview,
            ImageUrl = !string.IsNullOrEmpty(details.PosterPath) 
                ? $"https://image.tmdb.org/t/p/w500{details.PosterPath}" 
                : null,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Director = GetDirector(details.Credits),
            Cast = GetCast(details.Credits),
            ReleaseDate = ParseDate(details.ReleaseDate),
            RuntimeMinutes = details.Runtime,
            Genre = details.Genres?.FirstOrDefault()?.Name,
            Rating = null // TMDb vote_average is 0-10, could map if needed
        };
    }

    private string GetDirector(TmdbCredits? credits)
    {
        var director = credits?.Crew?.FirstOrDefault(c => c.Job == "Director");
        return director?.Name ?? "Unknown";
    }

    private string GetCast(TmdbCredits? credits)
    {
        if (credits?.Cast == null) return "";
        return string.Join(", ", credits.Cast.Take(5).Select(c => c.Name));
    }

    private DateTime? ParseDate(string? date)
    {
        if (DateTime.TryParse(date, out var dt)) return dt;
        return null;
    }

    // DTOs
    private class UpcResponse
    {
        public UpcItem[]? Items { get; set; }
    }

    private class UpcItem
    {
        public string? Title { get; set; }
    }

    private class TmdbSearchResponse
    {
        public TmdbResult[]? Results { get; set; }
    }

    private class TmdbResult
    {
        public int Id { get; set; }
    }

    private class TmdbMovieDetails
    {
        public string? Title { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }
        public TmdbGenre[]? Genres { get; set; }
        public TmdbCredits? Credits { get; set; }
    }

    private class TmdbGenre
    {
        public string? Name { get; set; }
    }

    private class TmdbCredits
    {
        public TmdbCast[]? Cast { get; set; }
        public TmdbCrew[]? Crew { get; set; }
    }

    private class TmdbCast
    {
        public string? Name { get; set; }
    }

    private class TmdbCrew
    {
        public string? Name { get; set; }
        public string? Job { get; set; }
    }
}
