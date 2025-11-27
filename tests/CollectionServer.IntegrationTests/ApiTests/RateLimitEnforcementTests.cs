using CollectionServer.Api.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Xunit;

namespace CollectionServer.IntegrationTests.ApiTests;

public class RateLimitEnforcementTests
{
    [Fact]
    public async Task FixedWindowLimiter_Rejects_Request_After_Permit_Limit()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "3",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:QueueLimit"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRateLimitingServices(configuration);

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        options.GlobalLimiter.Should().NotBeNull();
        options.OnRejected.Should().NotBeNull();

        static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            return context;
        }

        var leases = new List<RateLimitLease>();
        for (var i = 0; i < 3; i++)
        {
            var lease = await options.GlobalLimiter!.AcquireAsync(CreateContext(), 1);
            lease.IsAcquired.Should().BeTrue();
            leases.Add(lease);
        }

        var rejectedRequestContext = CreateContext();
        var rejectedLease = await options.GlobalLimiter!.AcquireAsync(rejectedRequestContext, 1);
        rejectedLease.IsAcquired.Should().BeFalse();

        foreach (var lease in leases)
        {
            lease.Dispose();
        }

        await using var responseStream = new MemoryStream();
        rejectedRequestContext.Response.Body = responseStream;

        var onRejected = options.OnRejected!;
        var middlewareContext = new OnRejectedContext
        {
            HttpContext = rejectedRequestContext,
            Lease = rejectedLease
        };
        await onRejected(middlewareContext, CancellationToken.None);

        rejectedRequestContext.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        rejectedRequestContext.Response.Headers.Should().ContainKey("Retry-After");
        rejectedRequestContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        rejectedRequestContext.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        rejectedRequestContext.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
        rejectedRequestContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("3");
        rejectedRequestContext.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("0");

        responseStream.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<RateLimitResponse>(responseStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        payload.Should().NotBeNull();
        payload!.StatusCode.Should().Be(429);
        payload.Message.Should().Contain("요청 제한을 초과");
        payload.Limit.Should().Be(3);
        payload.Remaining.Should().Be(0);
    }

    private sealed record RateLimitResponse(int StatusCode, string Message, int RetryAfterSeconds, int Limit, int Remaining);
}
