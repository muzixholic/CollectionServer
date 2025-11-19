using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CollectionServer.UnitTests.EdgeCases;

/// <summary>
/// 음악 앨범 엣지 케이스 테스트
/// T054.4: 컴필레이션, 다중 디스크, 트랙 없음, 재발매
/// </summary>
public class MusicAlbumEdgeCaseTests
{
    [Fact]
    public void MusicAlbum_ShouldHandleCompilationAlbum_WithVariousArtists()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Now That's What I Call Music! 100",
            Artist = "Various Artists", // Compilation
            MediaType = MediaType.MusicAlbum
        };

        // Assert
        album.Artist.Should().Be("Various Artists");
    }

    [Fact]
    public void MusicAlbum_ShouldHandleEmptyTrackList_WhenTracksAreNotAvailable()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Tracks = new List<Track>() // No tracks
        };

        // Assert
        album.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullTrackList_WhenTracksAreNotAvailable()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Tracks = null // Null tracks
        };

        // Assert
        album.Tracks.Should().BeNull();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleMultiDiscAlbum_WithManyTracks()
    {
        // Arrange
        var tracks = new List<Track>();
        
        // Disc 1: 15 tracks
        for (int i = 1; i <= 15; i++)
        {
            tracks.Add(new Track { TrackNumber = i, Title = $"Disc 1 Track {i}", DurationSeconds = 180 });
        }
        
        // Disc 2: 15 tracks
        for (int i = 1; i <= 15; i++)
        {
            tracks.Add(new Track { TrackNumber = i, Title = $"Disc 2 Track {i}", DurationSeconds = 180 });
        }

        // Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "The Wall",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Tracks = tracks // 30 tracks total
        };

        // Assert
        album.Tracks.Should().HaveCount(30);
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullLabel_WhenLabelIsUnknown()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Label = null // Unknown label
        };

        // Assert
        album.Label.Should().BeNull();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullReleaseDate_WhenDateIsUnknown()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            ReleaseDate = null // Unknown date
        };

        // Assert
        album.ReleaseDate.Should().BeNull();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleReissue_WithDifferentBarcode()
    {
        // Arrange & Act
        var original = new MusicAlbum
        {
            Barcode = "077774603225", // Original release
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            ReleaseDate = new DateTime(1973, 3, 1),
            MediaType = MediaType.MusicAlbum
        };

        var reissue = new MusicAlbum
        {
            Barcode = "602547924889", // 2016 Remaster
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            ReleaseDate = new DateTime(2016, 6, 17),
            MediaType = MediaType.MusicAlbum
        };

        // Assert
        original.Title.Should().Be(reissue.Title);
        original.Artist.Should().Be(reissue.Artist);
        original.Barcode.Should().NotBe(reissue.Barcode); // Different barcodes for different releases
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullGenre_WhenGenreIsNotCategorized()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Genre = null // No genre
        };

        // Assert
        album.Genre.Should().BeNull();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullImageUrl_WhenCoverIsNotAvailable()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            ImageUrl = null // No cover
        };

        // Assert
        album.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void MusicAlbum_ShouldHandleNullDescription_WhenDescriptionIsNotAvailable()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Description = null // No description
        };

        // Assert
        album.Description.Should().BeNull();
    }

    [Fact]
    public void Track_ShouldHandleZeroDuration_WhenDurationIsUnknown()
    {
        // Arrange & Act
        var track = new Track
        {
            TrackNumber = 1,
            Title = "Speak to Me",
            DurationSeconds = 0 // Unknown duration represented as 0
        };

        // Assert
        track.DurationSeconds.Should().Be(0);
    }

    [Theory]
    [InlineData(30)]     // Short intro
    [InlineData(180)]    // 3 minutes (typical)
    [InlineData(600)]    // 10 minutes (progressive rock)
    [InlineData(1800)]   // 30 minutes (live jam)
    public void Track_ShouldHandleVariousDurations_WhenDurationVaries(int duration)
    {
        // Arrange & Act
        var track = new Track
        {
            TrackNumber = 1,
            Title = "Test Track",
            DurationSeconds = duration
        };

        // Assert
        track.DurationSeconds.Should().Be(duration);
    }

    [Fact]
    public void MusicAlbum_ShouldHandleSpecialCharactersInTitle_WhenTitleContainsUnicode()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "8809534461132",
            Title = "LOVE YOURSELF 結 'Answer'", // Korean album with Japanese characters
            Artist = "방탄소년단 (BTS)",
            MediaType = MediaType.MusicAlbum
        };

        // Assert
        album.Title.Should().Contain("結");
        album.Artist.Should().Contain("방탄소년단");
    }

    [Fact]
    public void MusicAlbum_ShouldSetTimestamps_WhenCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Dark Side of the Moon",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var afterCreation = DateTime.UtcNow;

        // Assert
        album.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        album.CreatedAt.Should().BeOnOrBefore(afterCreation);
        album.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        album.UpdatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void MusicAlbum_ShouldHandleMultipleArtists_WhenArtistsAreCommaSeparated()
    {
        // Arrange & Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Watch the Throne",
            Artist = "Jay-Z, Kanye West", // Multiple artists
            MediaType = MediaType.MusicAlbum
        };

        // Assert
        album.Artist.Should().Contain(",");
        album.Artist.Split(',').Should().HaveCount(2);
    }

    [Fact]
    public void Track_ShouldHandleVeryLongTrackTitle_WhenTitleExceedsTypicalLength()
    {
        // Arrange
        var longTitle = new string('A', 200); // 200 characters

        // Act
        var track = new Track
        {
            TrackNumber = 1,
            Title = longTitle,
            DurationSeconds = 180
        };

        // Assert
        track.Title.Should().HaveLength(200);
    }

    [Fact]
    public void MusicAlbum_ShouldHandleTracksWithSameNumber_OnDifferentDiscs()
    {
        // Arrange
        var tracks = new List<Track>
        {
            new Track { TrackNumber = 1, Title = "Disc 1 Track 1", DurationSeconds = 180 },
            new Track { TrackNumber = 1, Title = "Disc 2 Track 1", DurationSeconds = 200 } // Same number, different disc
        };

        // Act
        var album = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "The Wall",
            Artist = "Pink Floyd",
            MediaType = MediaType.MusicAlbum,
            Tracks = tracks
        };

        // Assert
        album.Tracks.Should().HaveCount(2);
        album.Tracks!.Count(t => t.TrackNumber == 1).Should().Be(2); // Two tracks with number 1
    }

    [Fact]
    public void MusicAlbum_ShouldHandleSingleWithOneTrack_WhenAlbumIsASingle()
    {
        // Arrange & Act
        var single = new MusicAlbum
        {
            Barcode = "602547924889",
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            MediaType = MediaType.MusicAlbum,
            Tracks = new List<Track>
            {
                new Track { TrackNumber = 1, Title = "Bohemian Rhapsody", DurationSeconds = 354 }
            }
        };

        // Assert
        single.Tracks.Should().HaveCount(1);
    }
}
