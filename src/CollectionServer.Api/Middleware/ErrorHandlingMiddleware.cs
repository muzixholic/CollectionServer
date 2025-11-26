using System.Net;
using System.Text.Json;
using CollectionServer.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CollectionServer.Api.Middleware;

/// <summary>
/// 전역 예외 처리 미들웨어
/// RFC 7807 ProblemDetails 형식을 사용하여 에러 응답 반환
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
        var title = "서버 내부 오류";
        var detail = exception.Message;
        string? type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        switch (exception)
        {
            case InvalidBarcodeException invalidBarcodeEx:
                statusCode = HttpStatusCode.BadRequest;
                title = "잘못된 바코드 형식";
                detail = $"{invalidBarcodeEx.Message} 올바른 형식: ISBN-10 (10자리), ISBN-13 (13자리), UPC (12자리), EAN-13 (13자리)";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case NotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                title = "리소스를 찾을 수 없음";
                detail = notFoundEx.Message;
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                break;

            case RateLimitExceededException rateLimitEx:
                statusCode = HttpStatusCode.TooManyRequests;
                title = "요청 제한 초과";
                detail = "잠시 후 다시 시도해주세요.";
                type = "https://tools.ietf.org/html/rfc6585#section-4";
                context.Response.Headers.Append("Retry-After", rateLimitEx.RetryAfterSeconds.ToString());
                break;

            case ExternalApiException externalEx:
                statusCode = HttpStatusCode.BadGateway;
                title = "외부 서비스 오류";
                detail = $"외부 제공자({externalEx.ProviderName}) 오류: {externalEx.Message}";
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.3";
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path
        };
        
        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);
        problemDetails.Extensions.Add("timestamp", DateTime.UtcNow);

        return context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
    }
}
