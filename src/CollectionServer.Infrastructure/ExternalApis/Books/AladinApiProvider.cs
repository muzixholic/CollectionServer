using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace CollectionServer.Infrastructure.ExternalApis.Books;

/// <summary>
/// Aladin API 제공자 (도서, 음반, DVD/Blu-ray 지원)
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
        // ISBN (10/13 digits), UPC (12 digits), or EAN-13 (13 digits)
        if (cleaned.Length == 10) return true; // ISBN-10
        if (cleaned.Length == 12) return true; // UPC (Music)
        if (cleaned.Length == 13) return true; // ISBN-13, EAN-13 (Music/DVD)
        return false;
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            _logger.LogInformation("Querying Aladin API for barcode: {Barcode}", barcode);

            // Use ItemSearch API with Barcode query
            var url = $"{_settings.BaseUrl}?ttbkey={_settings.ApiKey}" +
                     $"&Query={HttpUtility.UrlEncode(barcode)}" +
                     $"&QueryType=Barcode" +
                     $"&SearchTarget=All" +
                     $"&MaxResults=1" +
                     $"&output=js" +
                     $"&Version=20131101";

            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Aladin API returned {StatusCode} for barcode: {Barcode}",
                    response.StatusCode, barcode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<AladinSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Item == null || result.Item.Length == 0)
            {
                _logger.LogInformation("No results found from Aladin API for barcode: {Barcode}", barcode);
                return null;
            }

            var item = result.Item[0];
            
            // Determine media type based on mallType
            MediaItem mediaItem = item.MallType?.ToUpperInvariant() switch
            {
                "MUSIC" => CreateMusicAlbum(item, barcode),
                "DVD" => CreateMovie(item, barcode),
                _ => CreateBook(item, barcode) // Default to book
            };

            _logger.LogInformation("Successfully retrieved {Type} from Aladin API: {Title}",
                mediaItem.MediaType, mediaItem.Title);
            
            return mediaItem;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to Aladin API timed out for barcode: {Barcode}", barcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Aladin API for barcode: {Barcode}", barcode);
            return null;
        }
    }

    private Book CreateBook(AladinItem item, string barcode)
    {
        return new Book
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.Book,
            Title = CleanTitle(item.Title),
            Description = item.Description,
            ImageUrl = item.Cover,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Isbn13 = item.Isbn13 ?? item.Isbn,
            Authors = item.Author,
            Publisher = item.Publisher,
            PublishDate = ParseDate(item.PubDate),
            PageCount = null,
            Genre = ExtractGenre(item.CategoryName)
        };
    }

    private MusicAlbum CreateMusicAlbum(AladinItem item, string barcode)
    {
        return new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.MusicAlbum,
            Title = CleanTitle(item.Title),
            Description = item.Description ?? $"Album by {item.Author}",
            ImageUrl = item.Cover,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Artist = item.Author ?? "Unknown Artist",
            Tracks = new List<Track>(), // Aladin doesn't provide track list
            ReleaseDate = ParseDate(item.PubDate),
            Label = item.Publisher,
            Genre = ExtractGenre(item.CategoryName)
        };
    }

    private Movie CreateMovie(AladinItem item, string barcode)
    {
        return new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            MediaType = MediaType.Movie,
            Title = CleanTitle(item.Title),
            Description = item.Description ?? $"Directed by {ExtractDirector(item.Author)}",
            ImageUrl = item.Cover,
            Source = ProviderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Director = ExtractDirector(item.Author),
            Cast = ExtractCast(item.Author),
            ReleaseDate = ParseDate(item.PubDate),
            RuntimeMinutes = null,
            Genre = ExtractGenre(item.CategoryName),
            Rating = null
        };
    }

    private static string CleanTitle(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "Unknown";
        
        // Remove prefixes like "[수입]", "[한정반]" etc
        var cleaned = title;
        if (cleaned.StartsWith("["))
        {
            var endBracket = cleaned.IndexOf("]");
            if (endBracket > 0 && endBracket < cleaned.Length - 1)
            {
                cleaned = cleaned.Substring(endBracket + 1).Trim();
            }
        }
        
        return cleaned;
    }

    private static string? ExtractGenre(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) return null;
        
        // Extract the last part of category as genre
        // e.g., "음반>팝>기획>죽기 전에 꼭 들어야 할 앨범 1001" -> "죽기 전에 꼭 들어야 할 앨범 1001"
        var parts = categoryName.Split('>');
        return parts.Length > 1 ? parts[^1].Trim() : categoryName;
    }

    private static string ExtractDirector(string? author)
    {
        if (string.IsNullOrEmpty(author)) return "Unknown";
        
        // Format: "크리스토퍼 놀란 (감독), ..."
        if (author.Contains("(감독)"))
        {
            var directorPart = author.Split(',')[0];
            return directorPart.Replace("(감독)", "").Trim();
        }
        
        return author.Split(',')[0].Trim();
    }

    private static string ExtractCast(string? author)
    {
        if (string.IsNullOrEmpty(author)) return string.Empty;
        
        // Format: "크리스토퍼 놀란 (감독), 마이클 케인, 매튜 맥커너히, ... (출연)"
        var cast = new List<string>();
        var parts = author.Split(',');
        
        foreach (var part in parts)
        {
            var cleaned = part.Replace("(출연)", "").Replace("(감독)", "").Trim();
            if (!string.IsNullOrEmpty(cleaned) && !part.Contains("(감독)"))
            {
                cast.Add(cleaned);
            }
        }
        
        return string.Join(", ", cast);
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var date) ? date : null;
    }

    // Response DTOs
    private class AladinSearchResponse
    {
        public AladinItem[]? Item { get; set; }
        public int? TotalResults { get; set; }
    }

    private class AladinItem
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? PubDate { get; set; }
        public string? Description { get; set; }
        public string? Isbn { get; set; }
        public string? Isbn13 { get; set; }
        public string? Cover { get; set; }
        public string? CategoryName { get; set; }
        public string? Publisher { get; set; }
        public string? MallType { get; set; } // "BOOK", "MUSIC", "DVD"
    }
}
