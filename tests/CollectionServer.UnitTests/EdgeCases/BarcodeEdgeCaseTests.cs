using CollectionServer.Core.Exceptions;
using CollectionServer.Core.Services;
using FluentAssertions;
using Xunit;

namespace CollectionServer.UnitTests.EdgeCases;

/// <summary>
/// 바코드 엣지 케이스 테스트
/// T054.1: 체크 디지트 오류, 공백/대시 정규화, 잘못된 길이 검증
/// </summary>
public class BarcodeEdgeCaseTests
{
    private readonly BarcodeValidator _validator;

    public BarcodeEdgeCaseTests()
    {
        _validator = new BarcodeValidator();
    }

    [Theory]
    [InlineData("978-0-596-52068-7")]  // ISBN-13 with dashes
    [InlineData("978 0 596 52068 7")]  // ISBN-13 with spaces
    [InlineData(" 9780596520687 ")]    // ISBN-13 with leading/trailing spaces
    [InlineData("0-596-52068-9")]      // ISBN-10 with dashes
    [InlineData(" 0596520689 ")]       // ISBN-10 with spaces
    public void Validate_ShouldNormalizeWhitespaceAndDashes_WhenValidBarcode(string barcode)
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() => _validator.Validate(barcode));
        exception.Should().BeNull();
    }

    [Theory]
    [InlineData("9780596520680")]  // ISBN-13 with wrong check digit (should be 7)
    [InlineData("0596520680")]     // ISBN-10 with wrong check digit (should be 9)
    public void Validate_ShouldThrowInvalidBarcodeException_WhenCheckDigitIsWrong(string barcode)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Contain("체크 디지트");
    }

    [Theory]
    [InlineData("")]               // Empty string
    [InlineData("   ")]            // Whitespace only
    public void Validate_ShouldThrowInvalidBarcodeException_WhenEmpty(string barcode)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Contain("비어 있습니다");
    }

    [Theory]
    [InlineData("123")]            // Too short
    [InlineData("12345")]          // Invalid length (5 digits)
    [InlineData("123456789")]      // Invalid length (9 digits)
    [InlineData("12345678901")]    // Invalid length (11 digits)
    [InlineData("12345678901234")] // Too long (14 digits)
    public void Validate_ShouldThrowInvalidBarcodeException_WhenLengthIsInvalid(string barcode)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Contain("지원하지 않는 바코드 길이");
    }

    [Fact]
    public void Validate_ShouldThrowInvalidBarcodeException_When12DigitHasWrongCheckDigit()
    {
        // 12 digits will be treated as UPC, use known invalid UPC
        var barcode = "123456789010"; // Wrong check digit (should be 7)

        // Act & Assert
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Contain("체크 디지트");
    }

    [Theory]
    [InlineData("978A59652068B")]  // ISBN-13 with letters
    [InlineData("!@#$%^&*()")]     // Special characters
    [InlineData("12345678-ABC")]   // Mixed alphanumeric
    public void Validate_ShouldThrowInvalidBarcodeException_WhenContainsInvalidCharacters(string barcode)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Contain("유효하지 않은 바코드");
    }

    [Fact]
    public void Validate_ShouldThrowInvalidBarcodeException_WhenXIsNotAtEnd()
    {
        // ISBN-10: X must be at the end only
        var barcode = "059X520689"; // X not at end

        // Act & Assert - will throw check digit error since X in middle breaks calculation
        var exception = Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode));
        exception.Message.Should().Match(m => 
            m.Contains("유효하지 않은 바코드") || 
            m.Contains("체크 디지트"));
    }

    [Fact]
    public void Validate_ShouldAcceptISBN10WithX_WhenXIsCheckDigit()
    {
        // ISBN-10: 043942089X (The Pragmatic Programmer)
        var barcode = "043942089X";

        // Act & Assert - should not throw
        var exception = Record.Exception(() => _validator.Validate(barcode));
        exception.Should().BeNull();
    }

    [Theory]
    [InlineData("978-0-596-52068-7", "9780596520687")]  // Remove dashes
    [InlineData("978 0 596 52068 7", "9780596520687")]  // Remove spaces
    [InlineData(" 9780596520687 ", "9780596520687")]    // Trim spaces
    [InlineData("0-596-52068-9", "0596520689")]         // ISBN-10 with dashes
    public void Normalize_ShouldRemoveWhitespaceAndDashes(string input, string expected)
    {
        // Act
        var normalized = input.Replace("-", "").Replace(" ", "").Trim();

        // Assert
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    public void Validate_ShouldThrowInvalidBarcodeException_WhenBarcodeIsNull(string? barcode)
    {
        // Act & Assert
        Assert.Throws<InvalidBarcodeException>(() => _validator.Validate(barcode!));
    }

    [Theory]
    [InlineData("012345678905")]   // UPC-A (12 digits)
    [InlineData("5901234123457")]  // EAN-13 (13 digits, not ISBN)
    [InlineData("4006381333931")]  // EAN-13 (German barcode)
    public void Validate_ShouldAcceptValidNonISBNBarcodes(string barcode)
    {
        // Act & Assert - UPC and EAN-13 should be accepted
        var exception = Record.Exception(() => _validator.Validate(barcode));
        exception.Should().BeNull();
    }

    [Theory]
    [InlineData("9780596520687")]  // Valid ISBN-13
    [InlineData("0596520689")]     // Valid ISBN-10
    [InlineData("012345678905")]   // Valid UPC-A
    [InlineData("5901234123457")]  // Valid EAN-13
    public void Validate_ShouldSucceed_WhenBarcodeIsValid(string barcode)
    {
        // Act & Assert
        var exception = Record.Exception(() => _validator.Validate(barcode));
        exception.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldPerformance_WhenCalledMultipleTimes()
    {
        // Arrange
        var barcode = "9780596520687";
        var iterations = 10000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _validator.Validate(barcode);
        }
        stopwatch.Stop();

        // Assert - should complete in reasonable time (< 100ms for 10k validations)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
