using System.Net;
using System.Text.Json;
using CollectionServer.IntegrationTests.Fixtures;
using Xunit;

namespace CollectionServer.IntegrationTests.ApiTests;

/// <summary>
/// 미디어 엔드포인트 E2E 통합 테스트
/// 실제 HTTP 요청부터 데이터베이스까지 전체 스택 테스트
/// </summary>
public class MediaEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MediaEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        // 각 테스트 전 데이터베이스 초기화
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task GET_Health_엔드포인트_정상_작동()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task GET_Items_유효하지않은_바코드_400()
    {
        // Arrange
        var invalidBarcode = "invalid123";

        // Act
        var response = await _client.GetAsync($"/items/{invalidBarcode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("바코드", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GET_Items_존재하지않는_유효한_바코드_404()
    {
        // Arrange
        var validButNonExistentBarcode = "9780000000002";

        // Act
        var response = await _client.GetAsync($"/items/{validButNonExistentBarcode}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("9788966262281")]  // ISBN-13
    [InlineData("8966262287")]     // ISBN-10
    public async Task GET_Items_유효한_ISBN_처리(string barcode)
    {
        // Act
        var response = await _client.GetAsync($"/items/{barcode}");

        // Assert
        // 데이터베이스에 없으므로 404 (외부 API 통합 전)
        // 또는 외부 API에서 가져와서 200
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task GET_Items_빈_바코드_404()
    {
        // Act
        var response = await _client.GetAsync("/items/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_특수문자_포함_바코드_400()
    {
        // Arrange
        var barcodeWithSpecialChars = "978-896-626-228-1";

        // Act
        var response = await _client.GetAsync($"/items/{barcodeWithSpecialChars}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_응답_JSON_형식()
    {
        // Arrange
        var barcode = "9788966262281";

        // Act
        var response = await _client.GetAsync($"/items/{barcode}");

        // Assert
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task GET_Items_잘못된_체크섬_400()
    {
        // Arrange
        var invalidChecksumBarcode = "9788966262282"; // 마지막 숫자가 잘못됨

        // Act
        var response = await _client.GetAsync($"/items/{invalidChecksumBarcode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("12345")]           // 너무 짧음
    [InlineData("12345678901234")]  // 너무 긺
    public async Task GET_Items_잘못된_길이_바코드_400(string barcode)
    {
        // Act
        var response = await _client.GetAsync($"/items/{barcode}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_대소문자_X_처리()
    {
        // Arrange
        var barcodeWithUpperX = "080442957X"; // ISBN-10 with X check digit

        // Act
        var response = await _client.GetAsync($"/items/{barcodeWithUpperX}");

        // Assert
        // 유효한 바코드이므로 400이 아니어야 함
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Items_에러응답에_상세정보_포함()
    {
        // Arrange
        var invalidBarcode = "abc123";

        // Act
        var response = await _client.GetAsync($"/items/{invalidBarcode}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // JSON 파싱 확인
        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task 여러_요청_동시_처리()
    {
        // Arrange
        var barcodes = new[]
        {
            "9788966262281",
            "9780134685991",
            "9780596007126"
        };

        // Act
        var tasks = barcodes.Select(barcode => 
            _client.GetAsync($"/items/{barcode}")).ToArray();
        
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            // 모든 요청이 처리됨 (400이나 500이 아님)
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        });
    }

    [Fact]
    public async Task Swagger_UI_접근_가능()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenAPI_JSON_접근_가능()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out _));
    }
}
