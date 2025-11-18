using CollectionServer.Infrastructure.ExternalApis.Books;
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

public class KakaoBookProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<KakaoBookProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;

    public KakaoBookProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<KakaoBookProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            KakaoBook = new KakaoBookSettings
            {
                BaseUrl = "https://dapi.kakao.com",
                ApiKey = "test-key",
                Priority = 2,
                TimeoutSeconds = 10
            }
        });
    }

    [Theory]
    [InlineData("9788972756194")]
    [InlineData("978-89-7275-619-4")]
    [InlineData("1234567890")]
    public void SupportsBarcode_ValidISBN_ReturnsTrue(string barcode)
    {
        var provider = new KakaoBookProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData("123456789012345678")]
    public void SupportsBarcode_InvalidBarcode_ReturnsFalse(string barcode)
    {
        var provider = new KakaoBookProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeFalse();
    }

    [Fact]
    public void Priority_ShouldReturnConfiguredValue()
    {
        var provider = new KakaoBookProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.Priority.Should().Be(2);
    }

    [Fact]
    public void ProviderName_ShouldReturnKakaoBook()
    {
        var provider = new KakaoBookProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.ProviderName.Should().Be("KakaoBook");
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_NotImplemented_ReturnsNull()
    {
        var provider = new KakaoBookProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("9788972756194");
        result.Should().BeNull();
    }
}
