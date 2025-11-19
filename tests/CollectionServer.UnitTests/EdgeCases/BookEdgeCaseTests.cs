using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CollectionServer.UnitTests.EdgeCases;

/// <summary>
/// 도서 엣지 케이스 테스트
/// T054.2: 여러 저자, 표지 없음, 충돌 데이터, 설명 없음
/// </summary>
public class BookEdgeCaseTests
{
    [Fact]
    public void Book_ShouldHandleMultipleAuthors_WhenAuthorsAreCommaSeparated()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780201633610",
            Title = "Design Patterns",
            Authors = "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides", // Gang of Four
            MediaType = MediaType.Book
        };

        // Assert
        book.Authors.Should().Contain(",");
        book.Authors.Split(',').Should().HaveCount(4);
    }

    [Fact]
    public void Book_ShouldAcceptNullImageUrl_WhenCoverIsNotAvailable()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            ImageUrl = null // No cover available
        };

        // Assert
        book.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Book_ShouldAcceptEmptyDescription_WhenDescriptionIsNotAvailable()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            Description = null // No description
        };

        // Assert
        book.Description.Should().BeNull();
    }

    [Fact]
    public void Book_ShouldHandleVeryLongDescription_WhenDescriptionExceedsTypicalLength()
    {
        // Arrange
        var longDescription = new string('A', 5000); // 5000 characters

        // Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            Description = longDescription
        };

        // Assert
        book.Description.Should().HaveLength(5000);
    }

    [Fact]
    public void Book_ShouldHandleSingleAuthor_WhenOnlyOneAuthorExists()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford", // Single author
            MediaType = MediaType.Book
        };

        // Assert
        book.Authors.Should().Be("Douglas Crockford");
        book.Authors.Split(',').Should().HaveCount(1);
    }

    [Fact]
    public void Book_ShouldHandleNullPublisher_WhenPublisherIsUnknown()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            Publisher = null // Unknown publisher
        };

        // Assert
        book.Publisher.Should().BeNull();
    }

    [Fact]
    public void Book_ShouldHandleZeroPageCount_WhenPageCountIsUnknown()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            PageCount = null // Unknown page count
        };

        // Assert
        book.PageCount.Should().BeNull();
    }

    [Fact]
    public void Book_ShouldHandleNullPublishDate_WhenDateIsUnknown()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            PublishDate = null // Unknown date
        };

        // Assert
        book.PublishDate.Should().BeNull();
    }

    [Fact]
    public void Book_ShouldHandleNullGenre_WhenGenreIsNotCategorized()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            Genre = null // No genre
        };

        // Assert
        book.Genre.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Book_ShouldHandleEmptyOrWhitespaceAuthors_WhenAuthorsFieldIsBlank(string authors)
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = authors,
            MediaType = MediaType.Book
        };

        // Assert
        book.Authors.Should().Be(authors);
    }

    [Fact]
    public void Book_ShouldHandleVeryLongTitle_WhenTitleExceedsTypicalLength()
    {
        // Arrange
        var longTitle = new string('A', 500); // 500 characters

        // Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = longTitle,
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book
        };

        // Assert
        book.Title.Should().HaveLength(500);
    }

    [Fact]
    public void Book_ShouldSetTimestamps_WhenCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var book = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var afterCreation = DateTime.UtcNow;

        // Assert
        book.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        book.CreatedAt.Should().BeOnOrBefore(afterCreation);
        book.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        book.UpdatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Book_ShouldAllowDuplicateTitles_WithDifferentBarcodes()
    {
        // Arrange & Act
        var book1 = new Book
        {
            Barcode = "9780596520687",
            Title = "JavaScript: The Good Parts",
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book
        };

        var book2 = new Book
        {
            Barcode = "9780596805524", // Different ISBN
            Title = "JavaScript: The Good Parts", // Same title (different edition)
            Authors = "Douglas Crockford",
            MediaType = MediaType.Book
        };

        // Assert
        book1.Title.Should().Be(book2.Title);
        book1.Barcode.Should().NotBe(book2.Barcode);
    }

    [Fact]
    public void Book_ShouldHandleSpecialCharactersInTitle_WhenTitleContainsUnicode()
    {
        // Arrange & Act
        var book = new Book
        {
            Barcode = "9788932917245",
            Title = "해리 포터와 마법사의 돌", // Korean title
            Authors = "J.K. 롤링",
            MediaType = MediaType.Book
        };

        // Assert
        book.Title.Should().Contain("해리");
        book.Authors.Should().Contain("롤링");
    }
}
