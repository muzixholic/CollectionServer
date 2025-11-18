using System.Net;
using System.Text.Json;
using CollectionServer.Core.Exceptions;
using CollectionServer.Api.Models;

namespace CollectionServer.Api.Middleware;

/// <summary>
/// 전역 예외 처리 미들웨어
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "예외가 발생했습니다: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "서버 내부 오류가 발생했습니다.";
        var details = exception.Message;

        switch (exception)
        {
            case InvalidBarcodeException invalidBarcodeEx:
                statusCode = HttpStatusCode.BadRequest;
                message = "잘못된 바코드 형식입니다.";
                details = $"{invalidBarcodeEx.Message} 올바른 형식: ISBN-10 (10자리), ISBN-13 (13자리), UPC (12자리), EAN-13 (13자리)";
                break;

            case NotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                message = "미디어 정보를 찾을 수 없습니다.";
                details = notFoundEx.Message;
                break;

            case RateLimitExceededException rateLimitEx:
                statusCode = HttpStatusCode.TooManyRequests;
                message = "요청 제한을 초과했습니다. 잠시 후 다시 시도해주세요.";
                details = rateLimitEx.Message;
                context.Response.Headers.Append("Retry-After", rateLimitEx.RetryAfterSeconds.ToString());
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = details,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
