using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CollectionServer.UnitTests.EdgeCases;

/// <summary>
/// 영화 엣지 케이스 테스트
/// T054.3: Blu-ray/DVD 구분, 여러 감독, 출연진 제한, 미등급
/// </summary>
public class MovieEdgeCaseTests
{
    [Fact]
    public void Movie_ShouldHandleMultipleDirectors_WhenDirectorsAreCommaSeparated()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Lana Wachowski, Lilly Wachowski", // Multiple directors
            MediaType = MediaType.Movie
        };

        // Assert
        movie.Director.Should().Contain(",");
        movie.Director.Split(',').Should().HaveCount(2);
    }

    [Fact]
    public void Movie_ShouldHandleNullCast_WhenCastInformationIsNotAvailable()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Cast = null // No cast info
        };

        // Assert
        movie.Cast.Should().BeNull();
    }

    [Fact]
    public void Movie_ShouldHandleLargeCast_WhenMovieHasManyActors()
    {
        // Arrange
        var largeCast = string.Join(", ", Enumerable.Range(1, 50).Select(i => $"Actor {i}"));

        // Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Avengers",
            Director = "Joss Whedon",
            MediaType = MediaType.Movie,
            Cast = largeCast // 50 actors
        };

        // Assert
        movie.Cast.Should().NotBeNullOrEmpty();
        movie.Cast!.Split(',').Should().HaveCount(50);
    }

    [Fact]
    public void Movie_ShouldHandleNullRating_WhenMovieIsUnrated()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Rating = null // Unrated
        };

        // Assert
        movie.Rating.Should().BeNull();
    }

    [Theory]
    [InlineData("G")]
    [InlineData("PG")]
    [InlineData("PG-13")]
    [InlineData("R")]
    [InlineData("NC-17")]
    [InlineData("전체 관람가")]
    [InlineData("12세 관람가")]
    [InlineData("15세 관람가")]
    [InlineData("청소년 관람불가")]
    public void Movie_ShouldAcceptVariousRatings_WhenRatingIsProvided(string rating)
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Rating = rating
        };

        // Assert
        movie.Rating.Should().Be(rating);
    }

    [Fact]
    public void Movie_ShouldHandleNullRuntimeMinutes_WhenRuntimeIsUnknown()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            RuntimeMinutes = null // Unknown runtime
        };

        // Assert
        movie.RuntimeMinutes.Should().BeNull();
    }

    [Theory]
    [InlineData(90)]    // Short film
    [InlineData(120)]   // Typical movie
    [InlineData(180)]   // Long movie
    [InlineData(238)]   // The Irishman (very long)
    public void Movie_ShouldHandleVariousRuntimes_WhenRuntimeVaries(int runtime)
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "Test Movie",
            Director = "Test Director",
            MediaType = MediaType.Movie,
            RuntimeMinutes = runtime
        };

        // Assert
        movie.RuntimeMinutes.Should().Be(runtime);
    }

    [Fact]
    public void Movie_ShouldHandleNullReleaseDate_WhenDateIsUnknown()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            ReleaseDate = null // Unknown date
        };

        // Assert
        movie.ReleaseDate.Should().BeNull();
    }

    [Fact]
    public void Movie_ShouldHandleNullGenre_WhenGenreIsNotCategorized()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Genre = null // No genre
        };

        // Assert
        movie.Genre.Should().BeNull();
    }

    [Fact]
    public void Movie_ShouldHandleNullImageUrl_WhenPosterIsNotAvailable()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            ImageUrl = null // No poster
        };

        // Assert
        movie.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Movie_ShouldHandleNullDescription_WhenSynopsisIsNotAvailable()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Description = null // No synopsis
        };

        // Assert
        movie.Description.Should().BeNull();
    }

    [Fact]
    public void Movie_ShouldDistinguishBetweenFormats_UsingDifferentBarcodes()
    {
        // Arrange & Act
        var dvd = new Movie
        {
            Barcode = "883929302123", // DVD UPC
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie
        };

        var bluray = new Movie
        {
            Barcode = "883929302456", // Blu-ray UPC (different)
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie
        };

        // Assert
        dvd.Title.Should().Be(bluray.Title);
        dvd.Barcode.Should().NotBe(bluray.Barcode); // Different formats have different barcodes
    }

    [Fact]
    public void Movie_ShouldHandleSpecialCharactersInTitle_WhenTitleContainsUnicode()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "기생충 (Parasite)", // Korean title
            Director = "봉준호",
            MediaType = MediaType.Movie
        };

        // Assert
        movie.Title.Should().Contain("기생충");
        movie.Director.Should().Contain("봉준호");
    }

    [Fact]
    public void Movie_ShouldSetTimestamps_WhenCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var afterCreation = DateTime.UtcNow;

        // Assert
        movie.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        movie.CreatedAt.Should().BeOnOrBefore(afterCreation);
        movie.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        movie.UpdatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Movie_ShouldHandleVeryLongDescription_WhenDescriptionExceedsTypicalLength()
    {
        // Arrange
        var longDescription = new string('A', 5000); // 5000 characters

        // Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Wachowski Brothers",
            MediaType = MediaType.Movie,
            Description = longDescription
        };

        // Assert
        movie.Description.Should().HaveLength(5000);
    }

    [Fact]
    public void Movie_ShouldHandleSingleDirector_WhenOnlyOneDirectorExists()
    {
        // Arrange & Act
        var movie = new Movie
        {
            Barcode = "883929302123",
            Title = "The Matrix",
            Director = "Lana Wachowski", // Single director
            MediaType = MediaType.Movie
        };

        // Assert
        movie.Director.Should().Be("Lana Wachowski");
        movie.Director.Split(',').Should().HaveCount(1);
    }
}
