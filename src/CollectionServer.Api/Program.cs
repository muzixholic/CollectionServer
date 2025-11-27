using CollectionServer.Api.Extensions;
using CollectionServer.Api.Middleware;
using CollectionServer.Core.Interfaces;
using CollectionServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// JSON 직렬화 설정 (Enum을 문자열로 변환)
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Serilog 로깅 구성
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/collectionserver-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 서비스 등록
// 개발/테스트 환경에서는 InMemory DB 사용 (EF Core 10 + Npgsql preview 호환성 문제 회피)
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<CollectionServer.Infrastructure.Data.ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("CollectionServerDev"));
    
    // Core 서비스 등록
    builder.Services.AddSingleton<CollectionServer.Core.Services.BarcodeValidator>();
    builder.Services.AddScoped<IMediaRepository, CollectionServer.Infrastructure.Repositories.MediaRepository>();
    builder.Services.AddScoped<IMediaService, CollectionServer.Core.Services.MediaService>();
}
else
{
    builder.Services.AddDatabaseServices(builder.Configuration);
}

// 외부 API 설정 및 Provider 등록 (모든 환경에서 필요)
builder.Services.AddExternalApiSettings(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);
builder.Services.AddHealthChecks();

var otlpEndpoint = builder.Configuration["Monitoring:OtlpExporter:Endpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("CollectionServer.Api", serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }
    });

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CollectionServer API",
        Version = "v1",
        Description = "미디어 정보 조회 API (도서, 영화, 음악 앨범)"
    });
});

// CORS (개발 및 테스트 환경)
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// 미들웨어 파이프라인
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseMiddleware<RateLimitMetricsMiddleware>();
app.MapPrometheusScrapingEndpoint();

// 헬스 체크 엔드포인트
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi(operation =>
   {
       operation.Summary = "헬스 체크";
       operation.Description = "API 서버 및 데이터베이스 연결 상태 확인";
       return operation;
   });

// 미디어 조회 엔드포인트
app.MapGet("/items/{barcode}", async (string barcode, IMediaService mediaService, CancellationToken cancellationToken) =>
{
    var mediaItem = await mediaService.GetMediaByBarcodeAsync(barcode, cancellationToken);
    return Results.Ok(mediaItem);
})
   .WithName("GetMediaByBarcode")
   .WithOpenApi(operation =>
   {
       operation.Summary = "바코드로 미디어 정보 조회";
       operation.Description = "ISBN, UPC, EAN-13 바코드로 도서, 영화, 음악 앨범 정보를 조회합니다. Database-First 방식으로 데이터베이스를 먼저 조회한 후 외부 API를 사용합니다.";
       return operation;
   })
   .Produces<CollectionServer.Core.Entities.MediaItem>(StatusCodes.Status200OK)
   .Produces(StatusCodes.Status400BadRequest)
   .Produces(StatusCodes.Status404NotFound)
   .Produces(StatusCodes.Status429TooManyRequests);

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/test/delay", async (int delayMs) =>
    {
        var safeDelay = Math.Clamp(delayMs, 0, 2000);
        if (safeDelay > 0)
        {
            await Task.Delay(safeDelay);
        }
        return Results.Ok(new { delayed = safeDelay });
    })
    .WithName("TestDelayEndpoint");
}

await app.RunAsync();

