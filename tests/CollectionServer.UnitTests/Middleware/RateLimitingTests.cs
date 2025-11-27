using CollectionServer.Api.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.Threading.RateLimiting;
using Xunit;

namespace CollectionServer.UnitTests.Middleware;

public class RateLimitingTests
{
    [Fact]
    public async Task OnRejected_Writes_Custom_Response_And_Headers()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "5",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:QueueLimit"] = "1"
            })
            .Build();

        services.AddRateLimitingServices(configuration);

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.OnRejected.Should().NotBeNull();

        var context = new DefaultHttpContext();
        await using var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = false
        });

        var acquiredLease = await limiter.AcquireAsync(1);
        acquiredLease.IsAcquired.Should().BeTrue();
        var rejectedLease = await limiter.AcquireAsync(1);
        rejectedLease.IsAcquired.Should().BeFalse();

        var rejectedContext = new OnRejectedContext
        {
            HttpContext = context,
            Lease = rejectedLease
        };

        await options.OnRejected!(rejectedContext, CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Headers["Retry-After"].ToString().Should().Be("60");
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("5");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("0");
        context.Response.Headers["X-RateLimit-Reset"].ToString().Should().NotBeNullOrEmpty();

        responseStream.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<RateLimitPayload>(responseStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        payload.Should().NotBeNull();
        payload!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        payload.Message.Should().Contain("요청 제한을 초과");
        payload.RetryAfterSeconds.Should().Be(60);
        payload.Limit.Should().Be(5);
        payload.Remaining.Should().Be(0);
    }

    private sealed record RateLimitPayload(int StatusCode, string Message, int RetryAfterSeconds, int Limit, int Remaining);
}
