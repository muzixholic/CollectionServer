using CollectionServer.Api.Middleware;
using CollectionServer.Api.Models;
using CollectionServer.Core.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CollectionServer.UnitTests.Middleware;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;
    private readonly DefaultHttpContext _httpContext;

    public ErrorHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_InvalidBarcodeException_Returns400()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new InvalidBarcodeException("Invalid barcode format"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new NotFoundException("Media", "12345"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_RateLimitExceededException_Returns429()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new RateLimitExceededException(60),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(429);
        _httpContext.Response.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task InvokeAsync_GeneralException_Returns500()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new Exception("Unexpected error"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var wasInvoked = false;
        var middleware = new ErrorHandlingMiddleware(
            _ => { wasInvoked = true; return Task.CompletedTask; },
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        wasInvoked.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_InvalidBarcodeException_IncludesKoreanMessage()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new InvalidBarcodeException("바코드 형식 오류"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("잘못된 바코드");
    }
}
