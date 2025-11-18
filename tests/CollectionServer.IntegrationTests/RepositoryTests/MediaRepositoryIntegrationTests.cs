using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Infrastructure.Data;
using CollectionServer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollectionServer.IntegrationTests.RepositoryTests;

/// <summary>
/// MediaRepository 실제 데이터베이스 통합 테스트
/// In-Memory DB를 사용하여 실제 EF Core 동작 검증
/// </summary>
public class MediaRepositoryIntegrationTests
{
    private DbContextOptions<ApplicationDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task 전체_CRUD_작업_시나리오()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "클린 코드",
            Authors = "로버트 C. 마틴",
            Publisher = "인사이트",
            PublishDate = new DateTime(2013, 12, 24),
            Isbn13 = "9788966262281",
            PageCount = 584,
            Description = "애자일 소프트웨어 장인 정신",
            MediaType = MediaType.Book
        };

        // Create
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.AddAsync(book);
            
            Assert.NotEqual(Guid.Empty, book.Id);
        }

        // Read
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var retrieved = await repository.GetByBarcodeAsync("9788966262281");
            
            Assert.NotNull(retrieved);
            Assert.Equal("클린 코드", retrieved.Title);
            Assert.IsType<Book>(retrieved);
        }

        // Update
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var existing = await repository.GetByBarcodeAsync("9788966262281");
            Assert.NotNull(existing);
            
            var bookToUpdate = (Book)existing;
            bookToUpdate.Description = "업데이트된 설명";
            await repository.UpdateAsync(bookToUpdate);
        }

        // Verify Update
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var updated = await repository.GetByBarcodeAsync("9788966262281");
            
            Assert.NotNull(updated);
            var updatedBook = (Book)updated;
            Assert.Equal("업데이트된 설명", updatedBook.Description);
        }

        // Delete
        Guid bookId;
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var existing = await repository.GetByBarcodeAsync("9788966262281");
            Assert.NotNull(existing);
            bookId = existing.Id;
            
            await repository.DeleteAsync(bookId);
        }

        // Verify Delete
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var deleted = await repository.GetByBarcodeAsync("9788966262281");
            
            Assert.Null(deleted);
        }
    }

    [Fact]
    public async Task 여러_미디어타입_저장_및_조회()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "클린 코드",
            Authors = "로버트 C. 마틴",
            MediaType = MediaType.Book
        };

        var movie = new Movie
        {
            Barcode = "012345678905",
            Title = "매트릭스",
            Director = "워쇼스키 자매",
            ReleaseDate = new DateTime(1999, 3, 31),
            MediaType = MediaType.Movie
        };

        var album = new MusicAlbum
        {
            Barcode = "8809479210654",
            Title = "테스트 앨범",
            Artist = "테스트 아티스트",
            ReleaseDate = new DateTime(2020, 1, 1),
            MediaType = MediaType.MusicAlbum
        };

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.AddAsync(book);
            await repository.AddAsync(movie);
            await repository.AddAsync(album);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            
            var retrievedBook = await repository.GetByBarcodeAsync("9788966262281");
            var retrievedMovie = await repository.GetByBarcodeAsync("012345678905");
            var retrievedAlbum = await repository.GetByBarcodeAsync("8809479210654");
            
            Assert.NotNull(retrievedBook);
            Assert.IsType<Book>(retrievedBook);
            Assert.Equal(MediaType.Book, retrievedBook.MediaType);
            
            Assert.NotNull(retrievedMovie);
            Assert.IsType<Movie>(retrievedMovie);
            Assert.Equal(MediaType.Movie, retrievedMovie.MediaType);
            
            Assert.NotNull(retrievedAlbum);
            Assert.IsType<MusicAlbum>(retrievedAlbum);
            Assert.Equal(MediaType.MusicAlbum, retrievedAlbum.MediaType);
        }
    }

    [Fact]
    public async Task Book_모든_필드_저장_및_조회()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "클린 코드",
            Authors = "로버트 C. 마틴",
            Publisher = "인사이트",
            PublishDate = new DateTime(2013, 12, 24),
            Isbn13 = "9788966262281",
            PageCount = 584,
            Description = "애자일 소프트웨어 장인 정신",
            ImageUrl = "https://example.com/cover.jpg",
            MediaType = MediaType.Book
        };

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.AddAsync(book);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var retrieved = await repository.GetByBarcodeAsync("9788966262281");
            
            Assert.NotNull(retrieved);
            var retrievedBook = (Book)retrieved;
            
            Assert.Equal("클린 코드", retrievedBook.Title);
            Assert.Equal("로버트 C. 마틴", retrievedBook.Authors);
            Assert.Equal("인사이트", retrievedBook.Publisher);
            Assert.Equal(new DateTime(2013, 12, 24), retrievedBook.PublishDate);
            Assert.Equal("9788966262281", retrievedBook.Isbn13);
            Assert.Equal(584, retrievedBook.PageCount);
            Assert.Equal("애자일 소프트웨어 장인 정신", retrievedBook.Description);
            Assert.Equal("https://example.com/cover.jpg", retrievedBook.ImageUrl);
        }
    }

    [Fact]
    public async Task Movie_모든_필드_저장_및_조회()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var movie = new Movie
        {
            Barcode = "012345678905",
            Title = "매트릭스",
            Director = "워쇼스키 자매",
            ReleaseDate = new DateTime(1999, 3, 31),
            RuntimeMinutes = 136,
            Description = "가상 현실 액션 영화",
            ImageUrl = "https://example.com/matrix.jpg",
            MediaType = MediaType.Movie
        };

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.AddAsync(movie);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var retrieved = await repository.GetByBarcodeAsync("012345678905");
            
            Assert.NotNull(retrieved);
            var retrievedMovie = (Movie)retrieved;
            
            Assert.Equal("매트릭스", retrievedMovie.Title);
            Assert.Equal("워쇼스키 자매", retrievedMovie.Director);
            Assert.Equal(new DateTime(1999, 3, 31), retrievedMovie.ReleaseDate);
            Assert.Equal(136, retrievedMovie.RuntimeMinutes);
        }
    }

    [Fact]
    public async Task MusicAlbum_트랙리스트_포함_저장_및_조회()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var album = new MusicAlbum
        {
            Barcode = "8809479210654",
            Title = "테스트 앨범",
            Artist = "테스트 아티스트",
            ReleaseDate = new DateTime(2020, 1, 1),
            Genre = "Rock",
            Tracks = new List<Track>
            {
                new() { Number = 1, Title = "첫 번째 곡", DurationSeconds = 180 },
                new() { Number = 2, Title = "두 번째 곡", DurationSeconds = 240 }
            },
            MediaType = MediaType.MusicAlbum
        };

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.AddAsync(album);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var retrieved = await repository.GetByBarcodeAsync("8809479210654");
            
            Assert.NotNull(retrieved);
            var retrievedAlbum = (MusicAlbum)retrieved;
            
            Assert.Equal("테스트 앨범", retrievedAlbum.Title);
            Assert.Equal("테스트 아티스트", retrievedAlbum.Artist);
            Assert.NotNull(retrievedAlbum.Tracks);
            Assert.Equal(2, retrievedAlbum.Tracks.Count);
            Assert.Equal("첫 번째 곡", retrievedAlbum.Tracks[0].Title);
            Assert.Equal(180, retrievedAlbum.Tracks[0].DurationSeconds);
        }
    }

    [Fact]
    public async Task 동시_조회_작업_처리()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var barcodes = new[] { "9788966262281", "9780134685991", "9780596007126" };
        
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            foreach (var barcode in barcodes)
            {
                await repository.AddAsync(new Book
                {
                    Barcode = barcode,
                    Title = $"테스트 도서 {barcode}",
                    MediaType = MediaType.Book
                });
            }
        }

        // Act
        var tasks = barcodes.Select(async barcode =>
        {
            await using var context = new ApplicationDbContext(options);
            var repository = new MediaRepository(context);
            return await repository.GetByBarcodeAsync(barcode);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.NotNull(result));
        Assert.Equal(barcodes.Length, results.Length);
    }
}
