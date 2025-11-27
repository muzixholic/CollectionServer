using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Models;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace CollectionServer.Infrastructure.ExternalApis.Movies;

/// <summary>
/// UPCitemdb를 사용하여 UPC/EAN 바코드를 영화 메타데이터로 해석하는 Resolver
/// </summary>
public class UpcItemDbResolver : IUpcResolver
{
    private static readonly Regex YearRegex = new("\\((\\d{4})\\)", RegexOptions.Compiled);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpcItemDbSettings _settings;
    private readonly TMDbSettings _tmdbSettings;
    private readonly ILogger<UpcItemDbResolver> _logger;
    private readonly ICacheService _cacheService;

    public UpcItemDbResolver(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ICacheService cacheService,
        ILogger<UpcItemDbResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.UpcItemDb;
        _tmdbSettings = settings.Value.TMDb;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<UpcResolutionResult?> ResolveAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeBarcode(barcode);
        if (normalized is null)
        {
            return null;
        }

        var cacheKey = $"upc-resolver:{normalized}";
        var cached = await _cacheService.GetAsync<UpcResolutionResult>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var upcItem = await GetUpcItemAsync(normalized, cancellationToken);
        if (upcItem is null)
        {
            return null;
        }

        var cleanTitle = CleanTitle(upcItem.Title);
        var releaseYear = ExtractYear(cleanTitle);
        var tmdbId = await SearchTmdbIdAsync(cleanTitle, releaseYear, cancellationToken);

        var result = new UpcResolutionResult
        {
            Barcode = normalized,
            Title = upcItem.Title ?? cleanTitle,
            CleanTitle = cleanTitle,
            ReleaseYear = releaseYear,
            Description = upcItem.Description,
            ImageUrl = upcItem.Images?.FirstOrDefault(),
            TmdbId = tmdbId,
            ImdbId = null
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(24), cancellationToken);
        return result;
    }

    private async Task<UpcItem?> GetUpcItemAsync(string barcode, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("UpcItemDb");
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

        return result?.Items?.FirstOrDefault();
    }

    private async Task<int?> SearchTmdbIdAsync(string? title, int? releaseYear, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var client = _httpClientFactory.CreateClient("TMDb");
        client.BaseAddress = new Uri(_tmdbSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_tmdbSettings.TimeoutSeconds);

        var query = $"/3/search/movie?api_key={_tmdbSettings.ApiKey}&query={HttpUtility.UrlEncode(title)}";
        if (releaseYear.HasValue)
        {
            query += $"&year={releaseYear.Value}";
        }

        var response = await client.GetAsync(query, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TMDb search failed with {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return payload?.Results?.FirstOrDefault()?.Id;
    }

    private static string? NormalizeBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var digits = new string(barcode.Where(char.IsDigit).ToArray());
        return digits.Length is 12 or 13 ? digits : null;
    }

    private static string? CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var cleaned = title;
        string[] suffixes = { "[Blu-ray]", "[DVD]", "(Blu-ray)", "(DVD)", "Blu-ray", "DVD", "4K Ultra HD" };

        foreach (var suffix in suffixes)
        {
            cleaned = cleaned.Replace(suffix, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return cleaned.Trim();
    }

    private static int? ExtractYear(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var match = YearRegex.Match(title);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var year))
        {
            return year;
        }

        return null;
    }

    private class UpcResponse
    {
        public UpcItem[]? Items { get; set; }
    }

    private class UpcItem
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string[]? Images { get; set; }
    }

    private class TmdbSearchResponse
    {
        public TmdbResult[]? Results { get; set; }
    }

    private class TmdbResult
    {
        public int Id { get; set; }
    }
}
