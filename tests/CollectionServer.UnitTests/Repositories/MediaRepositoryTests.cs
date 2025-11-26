using CollectionServer.Core.Entities;
using CollectionServer.Core.Enums;
using CollectionServer.Infrastructure.Data;
using CollectionServer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollectionServer.UnitTests.Repositories;

/// <summary>
/// MediaRepository 단위 테스트
/// CRUD 작업 및 데이터 액세스 로직 테스트
/// </summary>
public class MediaRepositoryTests
{
    private DbContextOptions<ApplicationDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByBarcodeAsync_존재하는_Book_반환()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "테스트 도서",
            Authors = "테스트 저자",
            MediaType = MediaType.Book
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(book);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("9788966262281");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("9788966262281", result.Barcode);
            Assert.Equal("테스트 도서", result.Title);
            Assert.IsType<Book>(result);
        }
    }

    [Fact]
    public async Task GetByBarcodeAsync_존재하지않음_Null_반환()
    {
        // Arrange
        var options = CreateInMemoryOptions();

        // Act
        await using var context = new ApplicationDbContext(options);
        var repository = new MediaRepository(context);
        var result = await repository.GetByBarcodeAsync("9780000000002");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBarcodeAsync_Movie_반환()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var movie = new Movie
        {
            Barcode = "012345678905",
            Title = "테스트 영화",
            Director = "테스트 감독",
            MediaType = MediaType.Movie
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(movie);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("012345678905");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Movie>(result);
            Assert.Equal(MediaType.Movie, result.MediaType);
        }
    }

    [Fact]
    public async Task GetByBarcodeAsync_MusicAlbum_반환()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var album = new MusicAlbum
        {
            Barcode = "8809479210654",
            Title = "테스트 앨범",
            Artist = "테스트 아티스트",
            MediaType = MediaType.MusicAlbum
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(album);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("8809479210654");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MusicAlbum>(result);
            Assert.Equal(MediaType.MusicAlbum, result.MediaType);
        }
    }

    [Fact]
    public async Task AddAsync_새_Book_추가()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "테스트 도서",
            Authors = "테스트 저자",
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
            var result = await repository.GetByBarcodeAsync("9788966262281");
            Assert.NotNull(result);
            Assert.Equal("테스트 도서", result.Title);
        }
    }

    [Fact]
    public async Task UpdateAsync_기존_Book_업데이트()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "원본 제목",
            Authors = "원본 저자",
            MediaType = MediaType.Book
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(book);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var existing = await repository.GetByBarcodeAsync("9788966262281");
            Assert.NotNull(existing);
            
            var bookToUpdate = (Book)existing;
            bookToUpdate.Title = "업데이트된 제목";
            await repository.UpdateAsync(bookToUpdate);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("9788966262281");
            Assert.NotNull(result);
            Assert.Equal("업데이트된 제목", result.Title);
        }
    }

    [Fact]
    public async Task DeleteAsync_기존_Book_삭제()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "테스트 도서",
            MediaType = MediaType.Book
        };

        Guid bookId;
        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(book);
            await context.SaveChangesAsync();
            bookId = book.Id;
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            await repository.DeleteAsync(bookId);
        }

        // Assert
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("9788966262281");
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task GetByBarcodeAsync_대소문자_구분없음()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "080442957X",
            Title = "테스트 도서",
            MediaType = MediaType.Book
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(book);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var result = await repository.GetByBarcodeAsync("080442957x");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("080442957X", result.Barcode);
        }
    }

    [Fact]
    public async Task GetByBarcodeAsync_여러_미디어타입_중_올바른것_반환()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var book = new Book
        {
            Barcode = "9788966262281",
            Title = "테스트 도서",
            MediaType = MediaType.Book
        };
        var movie = new Movie
        {
            Barcode = "012345678905",
            Title = "테스트 영화",
            MediaType = MediaType.Movie
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.MediaItems.Add(book);
            context.MediaItems.Add(movie);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new ApplicationDbContext(options))
        {
            var repository = new MediaRepository(context);
            var bookResult = await repository.GetByBarcodeAsync("9788966262281");
            var movieResult = await repository.GetByBarcodeAsync("012345678905");

            // Assert
            Assert.NotNull(bookResult);
            Assert.IsType<Book>(bookResult);
            Assert.NotNull(movieResult);
            Assert.IsType<Movie>(movieResult);
        }
    }
}
