using CollectionServer.Infrastructure.ExternalApis.Music;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace CollectionServer.UnitTests.ExternalApis;

public class MusicBrainzProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<MusicBrainzProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;

    public MusicBrainzProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<MusicBrainzProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            MusicBrainz = new MusicBrainzSettings
            {
                BaseUrl = "https://musicbrainz.org/ws/2",
                Priority = 1,
                TimeoutSeconds = 10
            }
        });
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("1234567890123")]
    public void SupportsBarcode_ValidUPCOrEAN_ReturnsTrue(string barcode)
    {
        var provider = new MusicBrainzProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("978123456789")]
    [InlineData("")]
    public void SupportsBarcode_InvalidBarcode_ReturnsFalse(string barcode)
    {
        var provider = new MusicBrainzProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeFalse();
    }

    [Fact]
    public void Priority_ShouldReturnConfiguredValue()
    {
        var provider = new MusicBrainzProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.Priority.Should().Be(1);
    }

    [Fact]
    public void ProviderName_ShouldReturnMusicBrainz()
    {
        var provider = new MusicBrainzProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.ProviderName.Should().Be("MusicBrainz");
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_NotImplemented_ReturnsNull()
    {
        var provider = new MusicBrainzProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("123456789012");
        result.Should().BeNull();
    }
}
