using System.Net;
using System.Text.Json;
using CollectionServer.ContractTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CollectionServer.ContractTests.Endpoints;

/// <summary>
/// 미디어 엔드포인트 계약 테스트
/// API 엔드포인트의 응답 형식, 상태 코드, 헤더 등이 계약을 준수하는지 검증
/// </summary>
public class MediaEndpointContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly ContractTestWebApplicationFactory _factory;

    public MediaEndpointContractTests(ContractTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GET_Items_유효하지않은_바코드_400_BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidBarcode = "12345"; // 너무 짧은 바코드

        // Act
        var response = await client.GetAsync($"/items/{invalidBarcode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_BadRequest_응답에_에러메시지_포함()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidBarcode = "invalid";

        // Act
        var response = await client.GetAsync($"/items/{invalidBarcode}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task GET_Items_응답_ContentType이_JSON()
    {
        // Arrange
        var client = _factory.CreateClient();
        var barcode = "9788966262281"; // 유효한 ISBN-13

        // Act
        var response = await client.GetAsync($"/items/{barcode}");

        // Assert
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task GET_Items_존재하지않는_미디어_404_NotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var barcode = "9780000000002"; // 유효하지만 존재하지 않는 ISBN

        // Act
        var response = await client.GetAsync($"/items/{barcode}");

        // Assert
        // 외부 API 통합 전에는 404, 통합 후에는 200 또는 404
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task GET_Health_200_OK_반환()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GET_Health_응답_형식_검증()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(content));
        
        // JSON 파싱 가능 여부 확인
        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.ValueKind == JsonValueKind.Object || 
                    doc.RootElement.ValueKind == JsonValueKind.String);
    }

    [Theory]
    [InlineData("9788966262281")]  // ISBN-13
    [InlineData("8966262287")]     // ISBN-10
    public async Task GET_Items_유효한_ISBN_처리(string barcode)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/items/{barcode}");

        // Assert
        // 유효한 바코드는 400이 아니어야 함 (200 또는 404)
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_빈_바코드_404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/items/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_하이픈포함_바코드_정상처리()
    {
        // Arrange
        var client = _factory.CreateClient();
        var barcodeWithHyphen = "978-896-626-228-1"; // 하이픈 포함 - 자동으로 제거되어 처리됨

        // Act
        var response = await client.GetAsync($"/items/{barcodeWithHyphen}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // 하이픈은 자동 제거되므로 유효한 바코드로 처리됨 (404 또는 200)
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 404 or 200, but got {response.StatusCode}. Content: {content}"
        );
    }
    
    [Fact]
    public async Task GET_Items_완전히_잘못된_바코드_400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidBarcode = "ABC-DEF-GHI"; // 숫자가 아닌 문자만

        // Act
        var response = await client.GetAsync($"/items/{invalidBarcode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_성공_응답에_미디어_정보_포함()
    {
        // Arrange
        var client = _factory.CreateClient();
        var barcode = "9788966262281"; // 유효한 ISBN-13

        // Act
        var response = await client.GetAsync($"/items/{barcode}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            
            // 기본 필드 존재 확인
            Assert.True(doc.RootElement.TryGetProperty("barcode", out _) ||
                       doc.RootElement.TryGetProperty("Barcode", out _));
        }
    }
}
