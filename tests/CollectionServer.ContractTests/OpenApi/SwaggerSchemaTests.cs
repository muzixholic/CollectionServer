using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CollectionServer.ContractTests.OpenApi;

/// <summary>
/// OpenAPI 스키마 검증 테스트
/// OpenAPI/Swagger 문서가 올바르게 생성되고 접근 가능한지 확인
/// </summary>
public class SwaggerSchemaTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerSchemaTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerUI_페이지_접근_가능()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", content);
    }

    [Fact]
    public async Task SwaggerJson_문서_생성됨()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out _));
        Assert.True(doc.RootElement.TryGetProperty("info", out _));
        Assert.True(doc.RootElement.TryGetProperty("paths", out _));
    }

    [Fact]
    public async Task OpenAPI_문서에_Items_엔드포인트_정의됨()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        var pathsJson = paths.GetRawText();
        Assert.Contains("/items/{barcode}", pathsJson);
    }

    [Fact]
    public async Task OpenAPI_문서에_Health_엔드포인트_정의됨()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        var pathsJson = paths.GetRawText();
        Assert.Contains("/health", pathsJson);
    }

    [Fact]
    public async Task OpenAPI_문서에_한국어_설명_포함()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // 한국어 문자가 포함되어 있는지 확인
        Assert.Matches(@"[\uAC00-\uD7A3]", content); // 한글 유니코드 범위
    }

    [Fact]
    public async Task OpenAPI_문서에_응답_스키마_정의됨()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("schemas", out _));
    }
}
