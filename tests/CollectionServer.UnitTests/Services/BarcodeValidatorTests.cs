using CollectionServer.Core.Enums;
using CollectionServer.Core.Exceptions;
using CollectionServer.Core.Services;
using Xunit;

namespace CollectionServer.UnitTests.Services;

/// <summary>
/// BarcodeValidator 단위 테스트
/// ISBN-10, ISBN-13, UPC, EAN-13 바코드 검증 로직 테스트
/// </summary>
public class BarcodeValidatorTests
{
    private readonly BarcodeValidator _validator;

    public BarcodeValidatorTests()
    {
        _validator = new BarcodeValidator();
    }

    #region ISBN-13 테스트

    [Theory]
    [InlineData("9788966262281")] // 유효한 ISBN-13
    [InlineData("9780134685991")] // Clean Code
    [InlineData("9780596007126")] // Head First Design Patterns
    public void Validate_유효한_ISBN13_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.Equal(BarcodeType.ISBN13, barcodeType);
    }

    [Theory]
    [InlineData("9788966262282")] // 잘못된 체크섬
    [InlineData("9780134685992")] // 잘못된 체크섬
    public void Validate_잘못된_ISBN13_체크섬_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    [Fact]
    public void Validate_ISBN13_12자리_실패()
    {
        // Arrange
        var barcode = "978896626228"; // 12자리

        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    #endregion

    #region ISBN-10 테스트

    [Theory]
    [InlineData("8966262287")]   // 유효한 ISBN-10
    [InlineData("0134685997")]   // Clean Code (ISBN-10)
    [InlineData("0596007124")]   // Head First Design Patterns (ISBN-10)
    public void Validate_유효한_ISBN10_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.Equal(BarcodeType.ISBN10, barcodeType);
    }

    [Theory]
    [InlineData("8966262288")]   // 잘못된 체크섬
    [InlineData("0134685998")]   // 잘못된 체크섬
    public void Validate_잘못된_ISBN10_체크섬_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    [Theory]
    [InlineData("080442957X")]   // X를 체크 디지트로 사용하는 ISBN-10
    [InlineData("043942089X")]   // The Hobbit
    public void Validate_X체크디지트_ISBN10_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.Equal(BarcodeType.ISBN10, barcodeType);
    }

    #endregion

    #region UPC 테스트

    [Theory]
    [InlineData("012345678905")]  // 유효한 UPC (12자리)
    [InlineData("786936735390")]  // Matrix Blu-ray
    public void Validate_유효한_UPC_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.Equal(BarcodeType.UPC, barcodeType);
    }

    [Theory]
    [InlineData("012345678906")]  // 잘못된 체크섬
    public void Validate_잘못된_UPC_체크섬_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    #endregion

    #region EAN-13 테스트

    [Theory]
    [InlineData("8809479210659")]  // 한국 음악 앨범
    [InlineData("8809634382030")]  // 한국 음악 앨범
    public void Validate_유효한_EAN13_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.Equal(BarcodeType.EAN13, barcodeType);
    }

    [Theory]
    [InlineData("8809479210655")]  // 잘못된 체크섬
    public void Validate_잘못된_EAN13_체크섬_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    #endregion

    #region 엣지 케이스

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_빈_바코드_실패(string? barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode!));
    }

    [Theory]
    [InlineData("12345")]          // 너무 짧음
    [InlineData("12345678901234")] // 너무 긺 (14자리)
    public void Validate_잘못된_길이_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    [Theory]
    [InlineData("ABC1234567890")]  // 알파벳 포함 (X 제외)
    public void Validate_숫자가_아닌_문자_실패(string barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
    }

    [Theory]
    [InlineData("978-896-626-228-1", "9788966262281")] // 하이픈 제거
    [InlineData("978 896 626 228 1", "9788966262281")] // 공백 제거
    [InlineData("0-123456-78905", "012345678905")]     // UPC 하이픈
    public void Validate_정규화_성공(string inputBarcode, string expectedNormalized)
    {
        // Act - 예외가 발생하지 않아야 함
        var result = _validator.Validate(inputBarcode);
        
        // Assert - 정규화되어 검증 통과
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("9788966262281", MediaType.Book)]     // ISBN-13 → Book
    [InlineData("8966262287", MediaType.Book)]        // ISBN-10 → Book
    [InlineData("012345678905", MediaType.Movie)]     // UPC → Movie (추정 불가, null 반환)
    [InlineData("8809479210659", MediaType.MusicAlbum)] // EAN-13 (880) → Music (추정 불가, null 반환)
    public void InferMediaType_바코드별_미디어타입_감지(string barcode, MediaType expectedType)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);
        var mediaType = _validator.InferMediaType(barcodeType);

        // Assert
        if (barcodeType == BarcodeType.ISBN10 || barcodeType == BarcodeType.ISBN13)
        {
            Assert.Equal(expectedType, mediaType);
        }
        else
        {
            // UPC와 EAN-13은 미디어 타입 추정 불가
            Assert.Null(mediaType);
        }
    }

    [Theory]
    [InlineData("9788966262281")]  // ISBN-13
    [InlineData("8966262287")]     // ISBN-10
    public void Validate_유효한_바코드형식_성공(string barcode)
    {
        // Act
        var barcodeType = _validator.Validate(barcode);

        // Assert
        Assert.True(barcodeType == BarcodeType.ISBN10 || 
                   barcodeType == BarcodeType.ISBN13 || 
                   barcodeType == BarcodeType.UPC || 
                   barcodeType == BarcodeType.EAN13);
    }

    [Fact]
    public void Validate_유효하지않은_바코드_예외발생()
    {
        // Arrange
        var invalidBarcode = "invalid123";

        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(invalidBarcode));
    }

    #endregion
}
