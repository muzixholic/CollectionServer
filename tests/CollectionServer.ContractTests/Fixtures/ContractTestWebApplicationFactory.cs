using CollectionServer.ContractTests.Fakes;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CollectionServer.ContractTests.Fixtures;

public class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddUserSecrets<Program>();
        });

        builder.ConfigureServices(services =>
        {
            Console.WriteLine("ContractTestWebApplicationFactory.ConfigureWebHost invoked");

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            RemoveDescriptor<ICacheService>(services);
            RemoveDescriptor<IConnectionMultiplexer>(services);

            services.AddSingleton<ICacheService, FakeCacheService>();
            services.AddTransient<MockHttpMessageHandler>();

            var providerNames = new[]
            {
                "GoogleBooks", "KakaoBook", "AladinApi",
                "TMDb", "OMDb", "UpcItemDb",
                "MusicBrainz", "Discogs"
            };

            foreach (var name in providerNames)
            {
                services.AddHttpClient(name)
                    .ConfigurePrimaryHttpMessageHandler<MockHttpMessageHandler>();
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }

    private static void RemoveDescriptor<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }
}
