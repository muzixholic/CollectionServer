using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Infrastructure.ExternalApis.Books;
using CollectionServer.Infrastructure.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace CollectionServer.UnitTests.ExternalApis;

public class GoogleBooksProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<GoogleBooksProvider>> _loggerMock;
    private readonly IOptions<ExternalApiSettings> _settings;
    private readonly GoogleBooksProvider _provider;

    public GoogleBooksProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<GoogleBooksProvider>>();
        _settings = Options.Create(new ExternalApiSettings
        {
            GoogleBooks = new GoogleBooksSettings
            {
                ApiKey = "test-api-key",
                BaseUrl = "https://www.googleapis.com/books/v1",
                Priority = 1,
                TimeoutSeconds = 10
            }
        });
        _provider = new GoogleBooksProvider(_httpClientFactoryMock.Object, _settings, _loggerMock.Object);
    }

    [Fact]
    public void ProviderName_Should_Return_GoogleBooks()
    {
        // Act
        var name = _provider.ProviderName;

        // Assert
        name.Should().Be("GoogleBooks");
    }

    [Fact]
    public void Priority_Should_Return_ConfiguredValue()
    {
        // Act
        var priority = _provider.Priority;

        // Assert
        priority.Should().Be(1);
    }

    [Theory]
    [InlineData("9780134685991")] // ISBN-13
    [InlineData("0134685997")] // ISBN-10
    public void SupportsBarcode_Should_Return_True_For_ISBN(string barcode)
    {
        // Act
        var supports = _provider.SupportsBarcode(barcode);

        // Assert
        supports.Should().BeTrue();
    }

    [Theory]
    [InlineData("012345678905")] // UPC
    [InlineData("0012345678905")] // EAN-13 (not ISBN)
    public void SupportsBarcode_Should_Return_False_For_Non_ISBN(string barcode)
    {
        // Act
        var supports = _provider.SupportsBarcode(barcode);

        // Assert
        supports.Should().BeFalse();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Should_Return_Book_On_Success()
    {
        // Arrange
        var isbn = "9780134685991";
        var responseJson = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    volumeInfo = new
                    {
                        title = "Effective Java",
                        authors = new[] { "Joshua Bloch" },
                        publisher = "Addison-Wesley",
                        publishedDate = "2018-01-06",
                        pageCount = 416,
                        description = "The Definitive Guide to Java Platform Best Practices",
                        imageLinks = new
                        {
                            thumbnail = "http://books.google.com/books/content?id=test&printsec=frontcover&img=1"
                        },
                        industryIdentifiers = new[]
                        {
                            new { type = "ISBN_13", identifier = "9780134685991" }
                        }
                    }
                }
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.Value.GoogleBooks.BaseUrl)
        };
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _provider.GetMediaByBarcodeAsync(isbn);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Book>();
        var book = result as Book;
        book!.Title.Should().Be("Effective Java");
        book.Authors.Should().Be("Joshua Bloch");
        book.Publisher.Should().Be("Addison-Wesley");
        book.PageCount.Should().Be(416);
        book.Isbn13.Should().Be("9780134685991");
        book.Barcode.Should().Be(isbn);
        book.MediaType.Should().Be(MediaType.Book);
        book.Source.Should().Be("GoogleBooks");
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Should_Return_Null_When_No_Items_Found()
    {
        // Arrange
        var isbn = "9999999999999";
        var responseJson = JsonSerializer.Serialize(new { totalItems = 0 });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.Value.GoogleBooks.BaseUrl)
        };
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _provider.GetMediaByBarcodeAsync(isbn);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Should_Return_Null_On_HTTP_Error()
    {
        // Arrange
        var isbn = "9780134685991";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.Value.GoogleBooks.BaseUrl)
        };
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _provider.GetMediaByBarcodeAsync(isbn);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Should_Handle_Timeout_Gracefully()
    {
        // Arrange
        var isbn = "9780134685991";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.Value.GoogleBooks.BaseUrl)
        };
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _provider.GetMediaByBarcodeAsync(isbn);

        // Assert
        result.Should().BeNull();
    }
}
