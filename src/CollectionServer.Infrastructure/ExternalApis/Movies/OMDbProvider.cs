using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Models;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Web;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// OMDb (Open Movie Database) API 제공자
/// </summary>
public class OMDbProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OMDbSettings _settings;
    private readonly IUpcResolver _upcResolver;
    private readonly ILogger<OMDbProvider> _logger;

    public OMDbProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        IUpcResolver upcResolver,
        ILogger<OMDbProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.OMDb;
        _upcResolver = upcResolver;
        _logger = logger;
    }

    public string ProviderName => "OMDb";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", string.Empty).Replace(" ", string.Empty);
        return cleaned.Length == 12 && cleaned.All(char.IsDigit);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var resolution = await _upcResolver.ResolveAsync(barcode, cancellationToken);
        if (resolution is null)
        {
            _logger.LogWarning("OMDbProvider - Unable to resolve UPC metadata for barcode: {Barcode}", barcode);
            return null;
        }

        var query = BuildQuery(resolution);
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("OMDbProvider - Missing query parameters for barcode: {Barcode}", barcode);
            return null;
        }

        var client = _httpClientFactory.CreateClient("OMDb");
        client.BaseAddress = new Uri(_settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        var response = await client.GetAsync(query, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OMDbProvider - HTTP {Status} for barcode {Barcode}", response.StatusCode, barcode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<OmdbResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload is null || !payload.IsSuccess)
        {
            _logger.LogWarning("OMDbProvider - API returned error: {Error}", payload?.Error);
            return null;
        }

        return new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.Movie,
            Title = payload.Title ?? resolution.CleanTitle ?? resolution.Title,
            Description = payload.Plot != "N/A" ? payload.Plot : resolution.Description,
            ImageUrl = payload.Poster != "N/A" ? payload.Poster : resolution.ImageUrl,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Director = payload.Director != "N/A" ? payload.Director : null,
            Cast = payload.Actors != "N/A" ? payload.Actors : null,
            ReleaseDate = ParseDate(payload.Released) ?? (resolution.ReleaseYear.HasValue ? new DateTime(resolution.ReleaseYear.Value, 1, 1) : null),
            RuntimeMinutes = ParseRuntime(payload.Runtime),
            Genre = payload.Genre != "N/A" ? payload.Genre : null,
            Rating = payload.Rated != "N/A" ? payload.Rated : null
        };
    }

    private string? BuildQuery(UpcResolutionResult resolution)
    {
        var parameters = new List<string> { $"apikey={_settings.ApiKey}" };

        if (!string.IsNullOrWhiteSpace(resolution.ImdbId))
        {
            parameters.Add($"i={resolution.ImdbId}");
        }
        else if (!string.IsNullOrWhiteSpace(resolution.CleanTitle ?? resolution.Title))
        {
            var title = resolution.CleanTitle ?? resolution.Title;
            parameters.Add($"t={HttpUtility.UrlEncode(title)}");
            if (resolution.ReleaseYear.HasValue)
            {
                parameters.Add($"y={resolution.ReleaseYear.Value}");
            }
        }
        else
        {
            return null;
        }

        parameters.Add("plot=short");
        return $"/?{string.Join("&", parameters)}";
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return DateTime.TryParse(value, out var date) ? date : null;
    }

    private static int? ParseRuntime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = value.Split(' ');
        if (int.TryParse(parts[0], out var minutes))
        {
            return minutes;
        }

        return null;
    }

    private class OmdbResponse
    {
        public string? Title { get; set; }
        public string? Plot { get; set; }
        public string? Poster { get; set; }
        public string? Released { get; set; }
        public string? Runtime { get; set; }
        public string? Genre { get; set; }
        public string? Director { get; set; }
        public string? Actors { get; set; }
        public string? Rated { get; set; }
        public string? Error { get; set; }
        public string? Response { get; set; }

        public bool IsSuccess => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);
    }
}
