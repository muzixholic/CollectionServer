using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Core.Exceptions;
using CollectionServer.Core.Interfaces;
using CollectionServer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CollectionServer.UnitTests.Services;

/// <summary>
/// MediaService 단위 테스트
/// Database-First 조회 로직 및 비즈니스 로직 테스트
/// </summary>
public class MediaServiceTests
{
    private readonly Mock<IMediaRepository> _mockRepository;
    private readonly BarcodeValidator _validator;
    private readonly Mock<ILogger<MediaService>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly MediaService _service;

    public MediaServiceTests()
    {
        _mockRepository = new Mock<IMediaRepository>();
        _validator = new BarcodeValidator();
        _mockLogger = new Mock<ILogger<MediaService>>();
        _mockCacheService = new Mock<ICacheService>();
        var emptyProviders = new List<IMediaProvider>();
        _service = new MediaService(
            _mockRepository.Object, 
            _validator, 
            emptyProviders, 
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_캐시에_존재_반환()
    {
        // Arrange
        var barcode = "9788966262281";
        var expectedBook = new Book
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Title = "캐시된 도서",
            MediaType = MediaType.Book
        };

        _mockCacheService
            .Setup(c => c.GetAsync<MediaItem>($"media:{barcode}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBook);

        // Act
        var result = await _service.GetMediaByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("캐시된 도서", result.Title);
        // Repository는 호출되지 않아야 함
        _mockRepository.Verify(r => r.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_데이터베이스에_존재_반환()
    {
        // Arrange
        var barcode = "9788966262281";
        var expectedBook = new Book
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Title = "테스트 도서",
            MediaType = MediaType.Book
        };

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBook);

        // Act
        var result = await _service.GetMediaByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(barcode, result.Barcode);
        Assert.Equal("테스트 도서", result.Title);
        _mockRepository.Verify(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_데이터베이스에_없음_NotFoundException()
    {
        // Arrange
        var barcode = "9780000000002";

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.GetMediaByBarcodeAsync(barcode));
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_잘못된_바코드_InvalidBarcodeException()
    {
        // Arrange
        var invalidBarcode = "invalid123";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidBarcodeException>(() => 
            _service.GetMediaByBarcodeAsync(invalidBarcode));
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_빈_바코드_InvalidBarcodeException()
    {
        // Arrange
        var emptyBarcode = "";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidBarcodeException>(() => 
            _service.GetMediaByBarcodeAsync(emptyBarcode));
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Repository_예외_전파()
    {
        // Arrange
        var barcode = "9788966262281";

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("데이터베이스 연결 실패"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetMediaByBarcodeAsync(barcode));
    }

    [Theory]
    [InlineData("9788966262281")] // ISBN-13
    [InlineData("8966262287")]    // ISBN-10
    [InlineData("012345678905")]  // UPC
    public async Task GetMediaByBarcodeAsync_다양한_바코드_타입_처리(string barcode)
    {
        // Arrange
        var mediaItem = new Book
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Title = "테스트",
            MediaType = MediaType.Book
        };

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaItem);

        // Act
        var result = await _service.GetMediaByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(barcode, result.Barcode);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_CancellationToken_전달()
    {
        // Arrange
        var barcode = "9788966262281";
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, token))
            .ReturnsAsync(new Book { Barcode = barcode });

        // Act
        await _service.GetMediaByBarcodeAsync(barcode, token);

        // Assert
        _mockRepository.Verify(r => r.GetByBarcodeAsync(barcode, token), Times.Once);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_Movie_엔티티_반환()
    {
        // Arrange
        var barcode = "012345678905";
        var expectedMovie = new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Title = "테스트 영화",
            MediaType = MediaType.Movie
        };

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMovie);

        // Act
        var result = await _service.GetMediaByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Movie>(result);
        Assert.Equal(MediaType.Movie, result.MediaType);
    }

    [Fact]
    public async Task GetMediaByBarcodeAsync_MusicAlbum_엔티티_반환()
    {
        // Arrange
        var barcode = "8809479210659"; // 수정된 유효한 체크섬
        var expectedAlbum = new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Title = "테스트 앨범",
            MediaType = MediaType.MusicAlbum
        };

        _mockRepository
            .Setup(r => r.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAlbum);

        // Act
        var result = await _service.GetMediaByBarcodeAsync(barcode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MusicAlbum>(result);
        Assert.Equal(MediaType.MusicAlbum, result.MediaType);
    }
}
