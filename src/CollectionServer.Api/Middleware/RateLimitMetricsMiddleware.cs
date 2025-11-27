using System.Diagnostics.Metrics;

namespace CollectionServer.Api.Middleware;

public class RateLimitMetricsMiddleware
{
    private static readonly Meter Meter = new("CollectionServer.RateLimiting");
    private static readonly Counter<long> RateLimitRejectionCounter = Meter.CreateCounter<long>("collectionserver_rate_limit_rejections_total");

    private readonly RequestDelegate _next;

    public RateLimitMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            RateLimitRejectionCounter.Add(1, new KeyValuePair<string, object?>("path", context.Request.Path.Value ?? string.Empty));
        }
    }
}
