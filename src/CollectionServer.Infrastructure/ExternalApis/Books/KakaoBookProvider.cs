using CollectionServer.Core.Entities;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CollectionServer.Infrastructure.ExternalApis.Books;

/// <summary>
/// Kakao Book Search API 제공자
/// </summary>
public class KakaoBookProvider : IMediaProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KakaoBookSettings _settings;
    private readonly ILogger<KakaoBookProvider> _logger;

    public KakaoBookProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApiSettings> settings,
        ILogger<KakaoBookProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value.KakaoBook;
        _logger = logger;
    }

    public string ProviderName => "KakaoBook";
    public int Priority => _settings.Priority;

    public bool SupportsBarcode(string barcode)
    {
        var cleaned = barcode.Replace("-", "").Replace(" ", "");
        return (cleaned.Length == 10 || cleaned.Length == 13) &&
               (cleaned.StartsWith("978") || cleaned.StartsWith("979") || cleaned.Length == 10);
    }

    public async Task<MediaItem?> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("KakaoBook");
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"KakaoAK {_settings.ApiKey}");

            var cleaned = barcode.Replace("-", "").Replace(" ", "");
            var response = await httpClient.GetAsync(
                $"/v3/search/book?query={cleaned}&target=isbn",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kakao Book API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<KakaoBookResponse>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Documents == null || result.Documents.Length == 0)
            {
                _logger.LogInformation("No results found for barcode: {Barcode}", barcode);
                return null;
            }

            var doc = result.Documents[0];
            return new Book
            {
                Barcode = cleaned,
                Title = doc.Title ?? "Unknown",
                Description = doc.Contents,
                ImageUrl = doc.Thumbnail,
                Source = ProviderName,
                Isbn13 = cleaned.Length == 13 ? cleaned : null,
                Authors = doc.Authors != null && doc.Authors.Length > 0 ? string.Join(", ", doc.Authors) : null,
                Publisher = doc.Publisher,
                PageCount = null,
                PublishDate = doc.Datetime,
                Genre = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching data from Kakao Book API for barcode: {Barcode}", barcode);
            return null;
        }
    }

    private class KakaoBookResponse
    {
        public KakaoBookDocument[]? Documents { get; set; }
    }

    private class KakaoBookDocument
    {
        public string? Title { get; set; }
        public string[]? Authors { get; set; }
        public string? Contents { get; set; }
        public string? Thumbnail { get; set; }
        public string? Publisher { get; set; }
        public DateTime? Datetime { get; set; }
    }
}
