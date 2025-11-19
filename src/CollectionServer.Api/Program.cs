using CollectionServer.Api.Extensions;
using CollectionServer.Api.Middleware;
using CollectionServer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog 로깅 구성
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/collectionserver-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 서비스 등록
// 개발 환경에서는 InMemory DB 사용 (EF Core 10 + Npgsql preview 호환성 문제 회피)
if (builder.Environment.IsDevelopment())
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
builder.Services.AddRateLimitingServices();

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

// CORS (개발 환경)
if (builder.Environment.IsDevelopment())
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

// 미들웨어 파이프라인
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseRateLimiter();

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
   .RequireRateLimiting("api")
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

app.Run();

