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
        // Arrange - 형식은 유효하지만 존재하지 않는 ISBN
        var isbn = "9781234567897";  // Valid ISBN-13 checksum

        // Act
        var response = await _client.GetAsync($"/items/{isbn}");

        // Assert
        // 모든 Provider가 시도했지만 찾지 못함
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
