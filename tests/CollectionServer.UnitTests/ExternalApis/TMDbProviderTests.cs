using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Models;
using CollectionServer.Infrastructure.ExternalApis.Movies;
using CollectionServer.Infrastructure.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace CollectionServer.UnitTests.ExternalApis;

public class TMDbProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IUpcResolver> _upcResolverMock = new();
    private readonly Mock<ILogger<TMDbProvider>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _tmdbHandlerMock = new();
    private readonly IOptions<ExternalApiSettings> _settings;

    public TMDbProviderTests()
    {
        _settings = Options.Create(new ExternalApiSettings
        {
            TMDb = new TMDbSettings
            {
                BaseUrl = "https://api.themoviedb.org/3",
                ApiKey = "test-key",
                Priority = 1,
                TimeoutSeconds = 10
            }
        });

        _httpClientFactoryMock.Setup(x => x.CreateClient("TMDb"))
            .Returns(() => new HttpClient(_tmdbHandlerMock.Object)
            {
                BaseAddress = new Uri(_settings.Value.TMDb.BaseUrl)
            });
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("1234567890123")]
    public void SupportsBarcode_ValidUPCOrEAN_ReturnsTrue(string barcode)
    {
        var provider = new TMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("9781234567897")]
    [InlineData("9791234567894")]
    [InlineData("")]
    public void SupportsBarcode_InvalidBarcode_ReturnsFalse(string barcode)
    {
        var provider = new TMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeFalse();
    }

    [Fact]
    public void Priority_ShouldMatchSettings()
    {
        var provider = new TMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        provider.Priority.Should().Be(1);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_ReturnsNull_WhenResolverFails()
    {
        _upcResolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpcResolutionResult?)null);

        var provider = new TMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_ReturnsMovie_WhenResolverProvidesTmdbId()
    {
        _upcResolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpcResolutionResult
            {
                Barcode = "123456789012",
                CleanTitle = "Interstellar",
                ReleaseYear = 2014,
                Description = "Resolver description",
                ImageUrl = "https://img",
                TmdbId = 157336
            });

        var tmdbDetailsResponse = new
        {
            id = 157336,
            title = "Interstellar",
            overview = "A sci-fi epic",
            poster_path = "/poster.jpg",
            release_date = "2014-11-05",
            runtime = 169,
            genres = new[] { new { name = "Adventure" } },
            credits = new
            {
                cast = new[] { new { name = "Matthew McConaughey" } },
                crew = new[] { new { name = "Christopher Nolan", job = "Director" } }
            }
        };

        _tmdbHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tmdbDetailsResponse))
            });

        var provider = new TMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");

        result.Should().NotBeNull();
        result!.Source.Should().Be("TMDb");
        result.Title.Should().Be("Interstellar");
    }
}
