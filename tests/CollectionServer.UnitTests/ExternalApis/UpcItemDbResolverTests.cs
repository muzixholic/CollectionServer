using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Models;
using CollectionServer.Infrastructure.ExternalApis.Movies;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CollectionServer.UnitTests.ExternalApis;

public class UpcItemDbResolverTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ILogger<UpcItemDbResolver>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _upcHandlerMock = new();
    private readonly Mock<HttpMessageHandler> _tmdbHandlerMock = new();
    private readonly IOptions<ExternalApiSettings> _settings;

    public UpcItemDbResolverTests()
    {
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

        _httpClientFactoryMock.Setup(x => x.CreateClient("UpcItemDb"))
            .Returns(() => new HttpClient(_upcHandlerMock.Object) { BaseAddress = new Uri(_settings.Value.UpcItemDb.BaseUrl) });

        _httpClientFactoryMock.Setup(x => x.CreateClient("TMDb"))
            .Returns(() => new HttpClient(_tmdbHandlerMock.Object) { BaseAddress = new Uri(_settings.Value.TMDb.BaseUrl) });
    }

    [Fact]
    public async Task ResolveAsync_Success_ReturnsResultAndCaches()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<UpcResolutionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpcResolutionResult?)null);

        _cacheServiceMock
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<UpcResolutionResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var upcResponse = new
        {
            items = new[] { new { title = "Interstellar [Blu-ray]", description = "A sci-fi epic", images = new[] { "https://img" } } }
        };

        var tmdbResponse = new
        {
            results = new[] { new { id = 157336, title = "Interstellar" } }
        };

        _upcHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(upcResponse))
            });

        _tmdbHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tmdbResponse))
            });

        var resolver = new UpcItemDbResolver(_httpClientFactoryMock.Object, _settings, _cacheServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("883929388292");

        // Assert
        result.Should().NotBeNull();
        result!.CleanTitle.Should().Be("Interstellar");
        result.TmdbId.Should().Be(157336);
        result.ImageUrl.Should().Be("https://img");
        _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<UpcResolutionResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UsesCache_WhenAvailable()
    {
        // Arrange
        var cached = new UpcResolutionResult { Barcode = "883929388292", CleanTitle = "Cached" };
        _cacheServiceMock
            .Setup(x => x.GetAsync<UpcResolutionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var resolver = new UpcItemDbResolver(_httpClientFactoryMock.Object, _settings, _cacheServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("883929388292");

        // Assert
        result.Should().Be(cached);
        _upcHandlerMock.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        _tmdbHandlerMock.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_UpcRequestFails_ReturnsNull()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<UpcResolutionResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpcResolutionResult?)null);

        _upcHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        var resolver = new UpcItemDbResolver(_httpClientFactoryMock.Object, _settings, _cacheServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("883929388292");

        // Assert
        result.Should().BeNull();
    }
}
