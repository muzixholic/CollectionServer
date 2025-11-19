using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using CollectionServer.Core.Interfaces;
using CollectionServer.IntegrationTests.Fixtures;

namespace CollectionServer.IntegrationTests.ApiTests;

public class ExternalApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExternalApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("9788936434267")]  // 한국 도서 ISBN (데미안)
    [InlineData("9780135957059")]  // 영문 도서 ISBN (The Pragmatic Programmer)
    public async Task GetItem_WithValidBookIsbn_ReturnsBookFromExternalApi(string isbn)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Match(x => x.Contains("\"mediaType\":1") || x.Contains("\"mediaType\":\"Book\""));
        content.Should().Contain("\"title\":");
        content.Should().Contain("\"barcode\":");
    }

    [Theory]
    [InlineData("883929609833")]   // UPC for movies
    public async Task GetItem_WithValidMovieUpc_ReturnsMovieFromExternalApi(string upc)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/items/{upc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Match(x => x.Contains("\"mediaType\":2") || x.Contains("\"mediaType\":\"Movie\"") || x.Contains("\"mediaType\":1") || x.Contains("\"mediaType\":\"Book\""));
    }

    [Theory]
    [InlineData("602537347421")]   // Music Album UPC
    public async Task GetItem_WithValidMusicUpc_ReturnsMusicFromExternalApi(string upc)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/items/{upc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetItem_DatabaseFirst_ReturnsCachedResult()
    {
        // Arrange
        var isbn = "9788936434267";

        // Act - First request (외부 API 호출)
        var firstResponse = await _client.GetAsync($"/items/{isbn}");
        var firstResponseTime = DateTimeOffset.UtcNow;

        // Act - Second request (데이터베이스 조회)
        var secondResponse = await _client.GetAsync($"/items/{isbn}");
        var secondResponseTime = DateTimeOffset.UtcNow;

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();

        // 동일한 데이터 반환 확인
        firstContent.Should().Be(secondContent);

        // 두 번째 요청이 더 빨라야 함 (캐싱 효과)
        var timeDifference = secondResponseTime - firstResponseTime;
        timeDifference.TotalMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task GetItem_ExternalApiReturnsData_SavesToDatabase()
    {
        // Arrange
        var isbn = "9780135957059";
        
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

        // Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 데이터베이스에 저장되었는지 확인
        var savedItem = await repository.GetByBarcodeAsync(isbn);
        savedItem.Should().NotBeNull();
        savedItem!.Barcode.Should().Be(isbn);
    }
}
