using CollectionServer.Infrastructure.ExternalApis.Music;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace CollectionServer.UnitTests.ExternalApis;

public class DiscogsProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DiscogsProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;

    public DiscogsProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<DiscogsProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            Discogs = new DiscogsSettings
            {
                BaseUrl = "https://api.discogs.com",
                ApiKey = "test-key",
                Priority = 2,
                TimeoutSeconds = 10
            }
        });
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("1234567890123")]
    public void SupportsBarcode_ValidUPCOrEAN_ReturnsTrue(string barcode)
    {
        var provider = new DiscogsProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("978123456789")]
    [InlineData("")]
    public void SupportsBarcode_InvalidBarcode_ReturnsFalse(string barcode)
    {
        var provider = new DiscogsProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeFalse();
    }

    [Fact]
    public void Priority_ShouldReturnConfiguredValue()
    {
        var provider = new DiscogsProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.Priority.Should().Be(2);
    }

    [Fact]
    public void ProviderName_ShouldReturnDiscogs()
    {
        var provider = new DiscogsProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.ProviderName.Should().Be("Discogs");
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_NotImplemented_ReturnsNull()
    {
        var provider = new DiscogsProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");
        result.Should().BeNull();
    }
}
