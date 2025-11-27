using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Models;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// TMDb (The Movie Database) API 제공자
/// </summary>
public class TMDbProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TMDbSettings _settings;
    private readonly IUpcResolver _upcResolver;
    private readonly ILogger<TMDbProvider> _logger;

    public TMDbProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        IUpcResolver upcResolver,
        ILogger<TMDbProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.TMDb;
        _upcResolver = upcResolver;
        _logger = logger;
    }

    public string ProviderName => "TMDb";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", string.Empty).Replace(" ", string.Empty);
        if (cleaned.Length == 12) return true;
        if (cleaned.Length == 13 && !cleaned.StartsWith("978") && !cleaned.StartsWith("979")) return true;
        return false;
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var resolution = await _upcResolver.ResolveAsync(barcode, cancellationToken);
        if (resolution is null)
        {
            _logger.LogWarning("TMDbProvider - Unable to resolve UPC metadata for barcode: {Barcode}", barcode);
            return null;
        }

        var tmdbId = resolution.TmdbId;
        if (!tmdbId.HasValue)
        {
            tmdbId = await SearchTmdbIdAsync(resolution.CleanTitle ?? resolution.Title, resolution.ReleaseYear, cancellationToken);
        }

        if (!tmdbId.HasValue)
        {
            _logger.LogWarning("TMDbProvider - Unable to map barcode {Barcode} to TMDb ID", barcode);
            return null;
        }

        return await GetTmdbDetailsAsync(tmdbId.Value, barcode, resolution, cancellationToken);
    }

    private async Task<int?> SearchTmdbIdAsync(string? title, int? releaseYear, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var client = _httpClientFactory.CreateClient("TMDb");
        client.BaseAddress = new Uri(_settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        var url = $"/3/search/movie?api_key={_settings.ApiKey}&query={HttpUtility.UrlEncode(title)}";
        if (releaseYear.HasValue)
        {
            url += $"&year={releaseYear.Value}";
        }

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TMDb search failed with status {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<TMDbSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Results?.FirstOrDefault()?.Id;
    }

    private async Task<MediaItem?> GetTmdbDetailsAsync(int movieId, string barcode, UpcResolutionResult resolution, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("TMDb");
        client.BaseAddress = new Uri(_settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        var url = $"/3/movie/{movieId}?api_key={_settings.ApiKey}&append_to_response=credits";
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TMDb detail lookup failed with status {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (details is null)
        {
            return null;
        }

        return new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.Movie,
            Title = details.Title ?? resolution.CleanTitle ?? resolution.Title,
            Description = details.Overview ?? resolution.Description,
            ImageUrl = !string.IsNullOrEmpty(details.PosterPath)
                ? $"https://image.tmdb.org/t/p/w500{details.PosterPath}"
                : resolution.ImageUrl,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Director = GetDirector(details.Credits),
            Cast = GetCast(details.Credits),
            ReleaseDate = ParseReleaseDate(details.ReleaseDate) ?? (resolution.ReleaseYear.HasValue
                ? new DateTime(resolution.ReleaseYear.Value, 1, 1)
                : null),
            RuntimeMinutes = details.Runtime,
            Genre = details.Genres?.FirstOrDefault()?.Name,
            Rating = null
        };
    }

    private static DateTime? ParseReleaseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var date) ? date : null;
    }

    private static string GetDirector(TMDbCredits? credits)
    {
        var director = credits?.Crew?.FirstOrDefault(c => c.Job == "Director");
        return director?.Name ?? "Unknown";
    }

    private static string GetCast(TMDbCredits? credits)
    {
        if (credits?.Cast == null) return string.Empty;
        return string.Join(", ", credits.Cast.Take(5).Select(c => c.Name));
    }

    private class TMDbSearchResponse
    {
        public TMDbMovie[]? Results { get; set; }
    }

    private class TMDbMovie
    {
        public int Id { get; set; }
        public string? Title { get; set; }
    }

    private class TMDbMovieDetails
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("release_date")]
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
