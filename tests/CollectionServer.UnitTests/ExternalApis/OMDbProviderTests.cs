using CollectionServer.Core.Entities;
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

public class OMDbProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IUpcResolver> _upcResolverMock = new();
    private readonly Mock<ILogger<OMDbProvider>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _omdbHandlerMock = new();
    private readonly IOptions<ExternalApiSettings> _settings;

    public OMDbProviderTests()
    {
        _settings = Options.Create(new ExternalApiSettings
        {
            OMDb = new OMDbSettings
            {
                BaseUrl = "http://www.omdbapi.com",
                ApiKey = "test-key",
                Priority = 2,
                TimeoutSeconds = 10
            }
        });

        _httpClientFactoryMock.Setup(x => x.CreateClient("OMDb"))
            .Returns(() => new HttpClient(_omdbHandlerMock.Object)
            {
                BaseAddress = new Uri(_settings.Value.OMDb.BaseUrl)
            });
    }

    [Theory]
    [InlineData("123456789012")]
    public void SupportsBarcode_ValidUPC_ReturnsTrue(string barcode)
    {
        var provider = new OMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeTrue();
    }

    [Theory]
    [InlineData("9781234567890")]
    [InlineData("1234567890123")]
    public void SupportsBarcode_Invalid_ReturnsFalse(string barcode)
    {
        var provider = new OMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        provider.SupportsBarcode(barcode).Should().BeFalse();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_ReturnsNull_WhenResolverReturnsNull()
    {
        _upcResolverMock.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpcResolutionResult?)null);

        var provider = new OMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_ReturnsMovie_WhenOmdbReturnsPayload()
    {
        _upcResolverMock.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpcResolutionResult
            {
                Barcode = "123456789012",
                CleanTitle = "Interstellar",
                ReleaseYear = 2014,
                Description = "Resolver Description",
                ImageUrl = "https://img"
            });

        var omdbResponse = new
        {
            Title = "Interstellar",
            Plot = "A sci-fi epic",
            Poster = "https://poster",
            Released = "07 Nov 2014",
            Runtime = "169 min",
            Genre = "Adventure",
            Director = "Christopher Nolan",
            Actors = "Matthew McConaughey",
            Rated = "PG-13",
            Response = "True"
        };

        _omdbHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(omdbResponse))
            });

        var provider = new OMDbProvider(_httpClientFactoryMock.Object, _settings, _upcResolverMock.Object, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");

        var movie = result.Should().BeOfType<Movie>().Subject;
        movie.Title.Should().Be("Interstellar");
        movie.Director.Should().Be("Christopher Nolan");
        movie.Source.Should().Be("OMDb");
    }
}
