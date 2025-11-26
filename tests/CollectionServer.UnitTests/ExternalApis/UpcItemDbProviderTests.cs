using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Infrastructure.ExternalApis.Movies;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CollectionServer.UnitTests.ExternalApis;

public class UpcItemDbProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<UpcItemDbProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public UpcItemDbProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<UpcItemDbProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            UpcItemDb = new UpcItemDbSettings
            {
                BaseUrl = "https://api.upcitemdb.com/prod/trial",
                Priority = 2,
                TimeoutSeconds = 10
            },
            TMDb = new TMDbSettings
            {
                BaseUrl = "https://api.themoviedb.org/3",
                ApiKey = "test-key",
                Priority = 1,
                TimeoutSeconds = 10
            }
        });

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.upcitemdb.com/prod/trial")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_httpMessageHandlerMock.Object) { BaseAddress = new Uri("https://api.upcitemdb.com/prod/trial") });
    }

    [Theory]
    [InlineData("883929388292")] // UPC 12
    [InlineData("8809507358292")] // EAN 13 (not 978/979)
    public void SupportsBarcode_Valid_ReturnsTrue(string barcode)
    {
        var provider = new UpcItemDbProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeTrue();
    }

    [Theory]
    [InlineData("9788966262281")] // ISBN 13
    [InlineData("123")] // Too short
    public void SupportsBarcode_Invalid_ReturnsFalse(string barcode)
    {
        var provider = new UpcItemDbProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeFalse();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Success_ReturnsMovie()
    {
        // Arrange
        var barcode = "883929388292";
        var title = "Interstellar";
        var movieId = 157336;

        // Mock UPCitemdb response
        var upcResponse = new
        {
            items = new[] { new { title = "Interstellar [Blu-ray]" } }
        };

        // Mock TMDb Search response
        var tmdbSearchResponse = new
        {
            results = new[] { new { id = movieId, title = "Interstellar" } }
        };

        // Mock TMDb Details response
        var tmdbDetailsResponse = new
        {
            id = movieId,
            title = "Interstellar",
            overview = "A team of explorers travel through a wormhole...",
            poster_path = "/gEU2QniL6C8zYEFeu32ULXq01ap.jpg",
            release_date = "2014-11-05",
            runtime = 169,
            genres = new[] { new { name = "Adventure" } },
            credits = new
            {
                cast = new[] { new { name = "Matthew McConaughey" } },
                crew = new[] { new { name = "Christopher Nolan", job = "Director" } }
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(upcResponse))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tmdbSearchResponse))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tmdbDetailsResponse))
            });

        var provider = new UpcItemDbProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);

        // Act
        var result = await provider.GetMediaByBarcodeAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Movie>();
        var movie = result as Movie;
        movie!.Title.Should().Be("Interstellar");
        movie.Director.Should().Be("Christopher Nolan");
        movie.Cast.Should().Contain("Matthew McConaughey");
        movie.RuntimeMinutes.Should().Be(169);
        movie.Source.Should().Be("UpcItemDb+TMDb");
    }
}
