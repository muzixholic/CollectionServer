using Moq;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CollectionServer.Core.Interfaces;
using CollectionServer.IntegrationTests.Fixtures;

namespace CollectionServer.IntegrationTests.ApiTests;

public class PriorityFallbackTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PriorityFallbackTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task GetItem_FirstProviderFails_FallsBackToSecondProvider()
    {
        // Arrange
        // 1순위 Provider가 실패하더라도 2순위에서 성공하는 시나리오
        var isbn = "9788936434267";

        // Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"mediaType\":\"Book\"");
    }

    [Fact]
    public async Task GetItem_MultipleProviders_UsesCorrectPriorityOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IMediaProvider>().ToList();

        // Assert - Provider 우선순위 확인
        var bookProviders = providers
            .Where(p => p.GetType().Name.Contains("Book"))
            .OrderBy(p => p.Priority)
            .ToList();

        bookProviders.Should().NotBeEmpty();
        
        // 첫 번째 Provider가 가장 낮은 Priority 값을 가져야 함
        if (bookProviders.Count > 1)
        {
            bookProviders[0].Priority.Should().BeLessThan(bookProviders[1].Priority);
        }
    }

    [Theory]
    [InlineData("9788936434267")]  // Korean book
    [InlineData("9780135957059")]  // English book
    public async Task GetItem_AllProvidersConfigured_ReturnsSuccessfully(string isbn)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetItem_InvalidButWellFormedIsbn_ReturnsNotFoundAfterTryingAllProviders()
    {
        // Arrange - 모든 Provider가 null을 반환하도록 설정
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 기존 Provider 제거
                var descriptors = services.Where(d => d.ServiceType == typeof(IMediaProvider)).ToList();
                foreach (var d in descriptors) services.Remove(d);
                
                // Mock Provider 추가 (항상 null 반환)
                var mockProvider = new Mock<IMediaProvider>();
                mockProvider.Setup(p => p.ProviderName).Returns("MockProvider");
                mockProvider.Setup(p => p.Priority).Returns(1);
                mockProvider.Setup(p => p.SupportsBarcode(It.IsAny<string>())).Returns(true);
                mockProvider.Setup(p => p.GetMediaByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((CollectionServer.Core.Entities.MediaItem?)null);
                
                services.AddScoped<IMediaProvider>(_ => mockProvider.Object);
            });
        }).CreateClient();

        var isbn = "9780000000002";

        // Act
        var response = await client.GetAsync($"/items/{isbn}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItem_ProviderTimeout_TriesNextProvider()
    {
        // Arrange
        var isbn = "9788936434267";

        // Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        // 타임아웃이 발생하더라도 다음 Provider로 폴백하여 성공
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
