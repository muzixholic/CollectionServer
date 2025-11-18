using CollectionServer.Infrastructure.ExternalApis.Books;
using CollectionServer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace CollectionServer.UnitTests.ExternalApis;

public class AladinApiProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<AladinApiProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;

    public AladinApiProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<AladinApiProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            AladinApi = new AladinApiSettings
            {
                BaseUrl = "http://www.aladin.co.kr/ttb/api",
                ApiKey = "test-key",
                Priority = 3,
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
        var provider = new AladinApiProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    public void SupportsBarcode_InvalidBarcode_ReturnsFalse(string barcode)
    {
        var provider = new AladinApiProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = provider.SupportsBarcode(barcode);
        result.Should().BeFalse();
    }

    [Fact]
    public void Priority_ShouldReturnConfiguredValue()
    {
        var provider = new AladinApiProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.Priority.Should().Be(3);
    }

    [Fact]
    public void ProviderName_ShouldReturnAladinApi()
    {
        var provider = new AladinApiProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        provider.ProviderName.Should().Be("AladinApi");
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_NotImplemented_ReturnsNull()
    {
        var provider = new AladinApiProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
        var result = await provider.GetMediaByBarcodeAsync("9788972756194");
        result.Should().BeNull();
    }
}
