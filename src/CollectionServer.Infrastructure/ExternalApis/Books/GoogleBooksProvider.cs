using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CollectionServer.Infrastructure.ExternalApis.Books;

/// <summary>
/// Google Books API 제공자
/// </summary>
public class GoogleBooksProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleBooksSettings _settings;
    private readonly ILogger<GoogleBooksProvider> _logger;

    public GoogleBooksProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<GoogleBooksProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.GoogleBooks;
        _logger = logger;
    }

    public string ProviderName => "GoogleBooks";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        // ISBN-10 or ISBN-13
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        return (cleaned.Length == 10 || cleaned.Length == 13) &&
               (cleaned.StartsWith("978") || cleaned.StartsWith("979") || cleaned.Length == 10);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GoogleBooks");
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            var url = $"/volumes?q=isbn:{barcode}";
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                url += $"&key={_settings.ApiKey}";
            }

            _logger.LogInformation("Querying Google Books API for ISBN: {Barcode}", barcode);

            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Books API returned {StatusCode} for ISBN: {Barcode}",
                    response.StatusCode, barcode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GoogleBooksResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Items == null || result.Items.Length == 0)
            {
                _logger.LogInformation("No results found from Google Books for ISBN: {Barcode}", barcode);
                return null;
            }

            var volumeInfo = result.Items[0].VolumeInfo;
            var isbn13 = volumeInfo.IndustryIdentifiers?
                .FirstOrDefault(id => id.Type == "ISBN_13")?.Identifier ?? barcode;

            var book = new Book
            {
                Id = Guid.NewGuid(),
                Barcode = barcode,
                MediaType = MediaType.Book,
                Title = volumeInfo.Title ?? "Unknown",
                Description = volumeInfo.Description,
                ImageUrl = volumeInfo.ImageLinks?.Thumbnail,
                Source = ProviderName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Isbn13 = isbn13,
                Authors = string.Join(", ", volumeInfo.Authors ?? Array.Empty<string>()),
                Publisher = volumeInfo.Publisher,
                PublishDate = ParsePublishDate(volumeInfo.PublishedDate),
                PageCount = volumeInfo.PageCount,
                Genre = volumeInfo.Categories?.FirstOrDefault()
            };

            _logger.LogInformation("Successfully retrieved book from Google Books: {Title}", book.Title);
            return book;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to Google Books API timed out for ISBN: {Barcode}", barcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Google Books API for ISBN: {Barcode}", barcode);
            return null;
        }
    }

    private static DateTime? ParsePublishDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var date) ? date : null;
    }

    // Response DTOs
    private class GoogleBooksResponse
    {
        public int TotalItems { get; set; }
        public GoogleBooksItem[]? Items { get; set; }
    }

    private class GoogleBooksItem
    {
        public VolumeInfo VolumeInfo { get; set; } = new();
    }

    private class VolumeInfo
    {
        public string? Title { get; set; }
        public string[]? Authors { get; set; }
        public string? Publisher { get; set; }
        public string? PublishedDate { get; set; }
        public string? Description { get; set; }
        public int? PageCount { get; set; }
        public string[]? Categories { get; set; }
        public ImageLinks? ImageLinks { get; set; }
        public IndustryIdentifier[]? IndustryIdentifiers { get; set; }
    }

    private class ImageLinks
    {
        public string? Thumbnail { get; set; }
    }

    private class IndustryIdentifier
    {
        public string Type { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
    }
}
