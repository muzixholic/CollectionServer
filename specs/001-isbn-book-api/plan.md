# 구현 계획 (Implementation Plan): 미디어 정보 API 서버

**브랜치 (Branch)**: `001-isbn-book-api` | **날짜 (Date)**: 2025-11-17 | **사양서 (Spec)**: [spec.md](./spec.md)  
**입력 (Input)**: Feature specification from `/specs/001-isbn-book-api/spec.md`

**참고 (Note)**: 이 문서는 ASP.NET Core 10 기반 구현을 위해 작성되었습니다.

## 요약 (Summary)

다양한 미디어 유형(도서, Blu-ray, DVD, 음악 앨범)의 바코드/ISBN을 입력받아 해당 미디어의 상세 정보를 반환하는 통합 REST API 서버를 ASP.NET Core 10으로 개발합니다. Database-First 아키텍처를 통해 PostgreSQL을 먼저 조회하고, 없을 경우 외부 API(Google Books, TMDb, MusicBrainz 등)를 우선순위에 따라 호출합니다. ASP.NET Core 10의 Minimal API 패턴, 내장 Rate Limiting, 의존성 주입을 활용하여 고성능 마이크로서비스를 구현합니다.

## 기술 컨텍스트 (Technical Context)

**언어/버전 (Language/Version)**: C# 13 / ASP.NET Core 10.0 (헌장 필수)  
**웹 프레임워크 (Web Framework)**: ASP.NET Core 10.0 Minimal APIs  
**주요 의존성 (Primary Dependencies)**:  
  - ASP.NET Core 10.0 (웹 프레임워크, 미들웨어 파이프라인)
  - Entity Framework Core 10.0 (ORM, 데이터 액세스)
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0 (PostgreSQL 드라이버)
  - Serilog.AspNetCore 10.0 (구조화된 로깅)
  - Swashbuckle.AspNetCore 7.0 (OpenAPI/Swagger 문서화)
  
**저장소 (Storage)**: PostgreSQL 16+ (주 데이터베이스, TPT 전략)  
**테스트 (Testing)**: xUnit 2.9, Moq 4.20, FluentAssertions 6.12, WebApplicationFactory (통합 테스트)  
**대상 플랫폼 (Target Platform)**: Linux server (Podman 컨테이너), 크로스 플랫폼 호환  
**프로젝트 유형 (Project Type)**: 단일 Web API 프로젝트 (백엔드 전용)  
**API 스타일 (API Style)**: Minimal API (vs Controller 기반) - 단일 엔드포인트 최적화  
**성능 목표 (Performance Goals)**: 
  - 데이터베이스 히트: <500ms 응답 시간
  - 외부 API 조회: <2초 응답 시간
  - 처리량: 100+ req/min per client (Rate Limiting)
  
**제약사항 (Constraints)**: 
  - 외부 API Rate Limit 준수 (Google Books: 1000/day, MusicBrainz: 1/sec)
  - 바코드 검증 필수 (체크섬 알고리즘)
  - 한국어 오류 메시지 및 로그
  
**규모/범위 (Scale/Scope)**: 
  - 단일 엔드포인트: GET /items/{barcode}
  - 4개 미디어 타입 지원 (Book, Blu-ray, DVD, Music)
  - 7개 외부 API 통합
  - 예상 데이터: 수만~수십만 미디어 항목

## 헌장 준수 검증 (Constitution Check)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 필수 검증 항목

- [x] **한국어 필수 (Principle I)**: 모든 문서가 한국어로 작성되는가?
  - ✅ spec.md: 한국어 작성 완료
  - ✅ plan.md: 한국어 작성 완료
  - ✅ research.md: 한국어 작성 완료
  - 🔄 코드 주석: 구현 시 복잡한 로직에 한국어 주석 추가 예정
  - 🔄 API 문서: OpenAPI 한국어 description 작성 예정
  - 🔄 에러 메시지: 모든 예외 메시지 한국어 작성 예정
  
- [x] **ASP.NET Core 10 스택 (Principle II)**: 기술 스택이 헌장을 준수하는가?
  - ✅ 백엔드 언어: C# 13 전용
  - ✅ 웹 프레임워크: ASP.NET Core 10.0
  - ✅ 런타임: .NET 10.0
  - ✅ ORM: Entity Framework Core 10.0
  - ✅ 다른 백엔드 언어 미사용
  - ✅ ASP.NET Core 핵심 기능 활용:
    - Minimal API 패턴
    - 의존성 주입 (DI Container)
    - 미들웨어 파이프라인
    - 내장 Rate Limiting
    - Configuration 시스템
    - Logging 추상화
  
- [x] **문서 완결성**: 모든 필수 섹션이 작성되었는가?
  - ✅ spec.md: 사용자 스토리, 요구사항, 엣지 케이스, 성공 기준
  - ✅ research.md: 기술 스택 결정, 아키텍처 패턴, NuGet 패키지
  - ✅ plan.md: 기술 컨텍스트, 프로젝트 구조, 복잡성 추적
  - 🔄 data-model.md: Phase 1에서 작성 예정
  - 🔄 contracts/: Phase 1에서 작성 예정
  - 🔄 quickstart.md: Phase 1에서 작성 예정

### ASP.NET Core 특화 검증

- [x] **API 스타일 결정**: Minimal API vs Controller 기반
  - ✅ 결정: **Minimal API** 선택
  - ✅ 근거: 단일 엔드포인트로 간결성 최대화, 낮은 메모리 사용량, .NET 10 최신 패턴
  - ✅ 대안 고려: Controller 기반은 복잡한 라우팅에 적합하나 본 프로젝트에는 과도

- [x] **의존성 주입 설계**: ASP.NET Core DI Container 활용
  - ✅ 서비스 등록: AddScoped, AddSingleton, AddHttpClient
  - ✅ 생성자 주입: IMediaService, IMediaRepository, ILogger
  - ✅ 옵션 패턴: IOptions<ExternalApiSettings> 사용

- [x] **미들웨어 파이프라인 설계**:
  - ✅ UseRateLimiter() - ASP.NET Core 10 내장
  - ✅ 전역 예외 처리 미들웨어 (ErrorHandlingMiddleware)
  - ✅ UseSerilog() - 구조화된 요청 로깅
  - ✅ UseSwagger() / UseSwaggerUI() - API 문서화

- [x] **Configuration 관리**:
  - ✅ appsettings.json: 기본 설정
  - ✅ appsettings.Development.json: 개발 환경
  - ✅ appsettings.Production.json: 프로덕션 환경
  - ✅ 환경 변수: 민감 정보 (DB 연결 문자열, API 키)
  - ✅ User Secrets: 로컬 개발용

### 위반사항 및 정당화

*헌장 위반사항 없음 - 모든 원칙 준수 ✅*

## 프로젝트 구조 (Project Structure)

### 문서 (이 기능용)

```text
specs/001-isbn-book-api/
├── spec.md              # 기능 명세서
├── plan.md              # 이 파일 - 구현 계획
├── research.md          # Phase 0 - 기술 연구 완료
├── data-model.md        # Phase 1 - 엔티티 설계
├── quickstart.md        # Phase 1 - 개발 환경 가이드
├── contracts/           # Phase 1 - OpenAPI 스키마
│   └── media-api.yaml
├── tasks.md             # Phase 2 - 작업 분해 (별도 명령)
└── checklists/          # 단계별 체크리스트
```

### 소스 코드 (Source Code - repository root)

**결정사항**: **옵션 1 (단일 프로젝트) 선택** - 백엔드 전용 Web API 서버

#### ASP.NET Core 솔루션 구조 (Clean Architecture)

```text
CollectionServer.sln                    # 솔루션 파일
global.json                             # .NET SDK 버전 고정

src/
├── CollectionServer.Api/               # 🌐 ASP.NET Core Web API 레이어
│   ├── Program.cs                      # Minimal API 엔드포인트, DI 설정, 미들웨어 파이프라인
│   ├── appsettings.json                # 기본 설정
│   ├── appsettings.Development.json    # 개발 환경 설정
│   ├── appsettings.Production.json     # 프로덕션 설정
│   ├── Middleware/                     # 커스텀 미들웨어
│   │   └── ErrorHandlingMiddleware.cs  # 전역 예외 처리
│   ├── Extensions/                     # 서비스 등록 확장 메서드
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── WebApplicationExtensions.cs
│   └── CollectionServer.Api.csproj     # API 프로젝트 파일
│
├── CollectionServer.Core/              # 📦 도메인 레이어 (비즈니스 로직)
│   ├── Entities/                       # 도메인 엔티티
│   │   ├── MediaItem.cs                # 추상 기본 클래스
│   │   ├── Book.cs                     # 도서 엔티티
│   │   ├── Movie.cs                    # 영화 엔티티
│   │   └── MusicAlbum.cs               # 음악 앨범 엔티티
│   ├── Interfaces/                     # 인터페이스 (DI용)
│   │   ├── IMediaService.cs            # 서비스 계약
│   │   ├── IMediaRepository.cs         # 저장소 계약
│   │   └── IMediaProvider.cs           # 외부 API Provider 계약
│   ├── Services/                       # 비즈니스 서비스
│   │   ├── MediaService.cs             # 메인 오케스트레이션 서비스
│   │   └── BarcodeValidator.cs         # 바코드 검증 서비스
│   ├── Enums/                          # 열거형
│   │   ├── MediaType.cs                # Book, Movie, Music
│   │   └── BarcodeType.cs              # ISBN10, ISBN13, UPC, EAN13
│   ├── Exceptions/                     # 도메인 예외
│   │   ├── NotFoundException.cs
│   │   ├── InvalidBarcodeException.cs
│   │   └── RateLimitExceededException.cs
│   └── CollectionServer.Core.csproj    # Core 프로젝트 파일
│
└── CollectionServer.Infrastructure/    # 🔌 인프라 레이어 (데이터 액세스, 외부 API)
    ├── Data/                           # EF Core 관련
    │   ├── ApplicationDbContext.cs     # DbContext
    │   ├── Configurations/             # Fluent API 설정
    │   │   ├── MediaItemConfiguration.cs
    │   │   ├── BookConfiguration.cs
    │   │   ├── MovieConfiguration.cs
    │   │   └── MusicAlbumConfiguration.cs
    │   └── Migrations/                 # EF Core 마이그레이션
    │       └── [자동 생성]
    ├── Repositories/                   # 저장소 구현
    │   └── MediaRepository.cs          # IMediaRepository 구현
    ├── ExternalApis/                   # 외부 API Provider 구현
    │   ├── Books/
    │   │   ├── GoogleBooksProvider.cs
    │   │   ├── KakaoBookProvider.cs
    │   │   └── AladinApiProvider.cs
    │   ├── Movies/
    │   │   ├── TMDbProvider.cs
    │   │   └── OMDbProvider.cs
    │   └── Music/
    │       ├── MusicBrainzProvider.cs
    │       └── DiscogsProvider.cs
    ├── Options/                        # 옵션 패턴 클래스
    │   └── ExternalApiSettings.cs      # API 키, Base URL 등
    └── CollectionServer.Infrastructure.csproj

tests/
├── CollectionServer.UnitTests/         # 🧪 단위 테스트 (xUnit)
│   ├── Services/
│   │   ├── MediaServiceTests.cs
│   │   └── BarcodeValidatorTests.cs
│   ├── Validators/
│   │   └── BarcodeTypeDetectionTests.cs
│   └── CollectionServer.UnitTests.csproj
│
├── CollectionServer.IntegrationTests/  # 🔗 통합 테스트 (WebApplicationFactory)
│   ├── ApiTests/
│   │   └── MediaEndpointTests.cs
│   ├── RepositoryTests/
│   │   └── MediaRepositoryTests.cs
│   ├── Fixtures/
│   │   └── TestWebApplicationFactory.cs  # 테스트용 In-Memory DB
│   └── CollectionServer.IntegrationTests.csproj
│
└── CollectionServer.ContractTests/     # 📋 계약 테스트
    ├── OpenApi/
    │   └── SwaggerSchemaTests.cs       # OpenAPI 스키마 검증
    └── CollectionServer.ContractTests.csproj
```

#### 주요 ASP.NET Core 파일 설명

| 파일/폴더 | 역할 | ASP.NET Core 특성 |
|----------|------|------------------|
| `Program.cs` | 애플리케이션 진입점 | WebApplicationBuilder, Minimal API 라우팅, 미들웨어 체인 |
| `appsettings.json` | 설정 파일 | IConfiguration 통합, 환경별 오버라이드 |
| `Middleware/` | 요청/응답 파이프라인 | IMiddleware 구현, UseMiddleware<T>() 등록 |
| `Extensions/` | DI 확장 메서드 | IServiceCollection 확장, 모듈화된 서비스 등록 |
| `ApplicationDbContext` | EF Core 컨텍스트 | DbContext, OnModelCreating, AddDbContext<T>() |
| `Configurations/` | EF Core Fluent API | IEntityTypeConfiguration<T> 구현 |
| `WebApplicationFactory` | 통합 테스트용 | ASP.NET Core 테스트 호스트, In-Memory 서버 |

### 구조 결정 근거

1. **Clean Architecture 적용**:
   - **API 레이어**: ASP.NET Core 특화 (Program.cs, Middleware, DI 설정)
   - **Core 레이어**: 프레임워크 독립적 비즈니스 로직
   - **Infrastructure 레이어**: EF Core, 외부 API, 데이터 액세스

2. **단일 솔루션 구조 선택 이유**:
   - 백엔드 전용 API 서버 (프론트엔드 분리)
   - 3개 프로젝트로 관심사 분리 (Api, Core, Infrastructure)
   - 테스트 프로젝트 독립 실행 (Unit, Integration, Contract)

3. **ASP.NET Core 10 패턴 반영**:
   - **Minimal API**: Program.cs에서 직접 라우팅 (MVC Controller 불필요)
   - **의존성 주입**: IServiceCollection 확장 메서드로 모듈화
   - **Options 패턴**: IOptions<T>로 강타입 설정 관리
   - **미들웨어 체인**: UseRateLimiter, UseSerilog, 커스텀 미들웨어 순서 제어

## 복잡성 추적 (Complexity Tracking)

> **헌장 준수 검증에서 위반사항이 있고 정당화가 필요한 경우에만 작성**

*헌장 위반사항 없음 - 복잡성 정당화 불필요* ✅

---

## ASP.NET Core 10 아키텍처 상세 (ASP.NET Core 10 Architecture Details)

### Minimal API 엔드포인트 설계

```csharp
// Program.cs - ASP.NET Core 10 Minimal API 패턴

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. 서비스 등록 (Dependency Injection)
// ========================================

// 데이터베이스 (Entity Framework Core 10 + PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 비즈니스 서비스
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddSingleton<BarcodeValidator>();

// 외부 API Providers (HttpClientFactory)
builder.Services.AddHttpClient<GoogleBooksProvider>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<TMDbProvider>();
builder.Services.AddHttpClient<MusicBrainzProvider>();
// ... 나머지 Providers

builder.Services.AddScoped<IMediaProvider, GoogleBooksProvider>();
builder.Services.AddScoped<IMediaProvider, KakaoBookProvider>();
// ... 우선순위에 따라 등록

// Rate Limiting (ASP.NET Core 10 내장)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
});

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "미디어 정보 API",
        Version = "v1",
        Description = "바코드/ISBN 기반 미디어 정보 조회 API"
    });
});

// Logging (Serilog)
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day);
});

// Options Pattern (강타입 설정)
builder.Services.Configure<ExternalApiSettings>(
    builder.Configuration.GetSection("ExternalApis"));

var app = builder.Build();

// ========================================
// 2. 미들웨어 파이프라인 (순서 중요!)
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // HTTP 요청 로깅

app.UseMiddleware<ErrorHandlingMiddleware>(); // 전역 예외 처리

app.UseRateLimiter(); // 속도 제한

// ========================================
// 3. Minimal API 엔드포인트 정의
// ========================================

app.MapGet("/", () => Results.Ok(new
{
    service = "CollectionServer Media API",
    version = "1.0.0",
    description = "바코드/ISBN 기반 미디어 정보 조회"
}))
.WithName("Root")
.WithOpenApi();

app.MapGet("/items/{barcode}", async (
    string barcode,
    IMediaService mediaService,
    ILogger<Program> logger) =>
{
    logger.LogInformation("미디어 조회 요청: {Barcode}", barcode);
    
    try
    {
        var result = await mediaService.GetMediaByBarcodeAsync(barcode);
        return Results.Ok(result);
    }
    catch (InvalidBarcodeException ex)
    {
        logger.LogWarning(ex, "잘못된 바코드 형식: {Barcode}", barcode);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (NotFoundException ex)
    {
        logger.LogInformation("미디어를 찾을 수 없음: {Barcode}", barcode);
        return Results.NotFound(new { error = ex.Message });
    }
})
.RequireRateLimiting("api")
.WithName("GetMediaByBarcode")
.WithOpenApi(operation => new(operation)
{
    Summary = "바코드로 미디어 정보 조회",
    Description = "ISBN-10/13, UPC, EAN-13 바코드를 사용하여 도서, 영화, 음악 정보를 조회합니다."
})
.Produces<MediaItem>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests);

// Health Check
app.MapGet("/health", async (ApplicationDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect 
        ? Results.Ok(new { status = "Healthy", database = "Connected" })
        : Results.ServiceUnavailable();
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
```

### 의존성 주입 패턴 (Dependency Injection Patterns)

#### 1. 생성자 주입 (Constructor Injection)

```csharp
public class MediaService : IMediaService
{
    private readonly IMediaRepository _repository;
    private readonly IEnumerable<IMediaProvider> _providers;
    private readonly BarcodeValidator _validator;
    private readonly ILogger<MediaService> _logger;
    
    // ASP.NET Core DI Container가 자동으로 인스턴스 주입
    public MediaService(
        IMediaRepository repository,
        IEnumerable<IMediaProvider> providers, // 여러 Provider 동시 주입
        BarcodeValidator validator,
        ILogger<MediaService> logger)
    {
        _repository = repository;
        _providers = providers;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<MediaItem> GetMediaByBarcodeAsync(string barcode)
    {
        // Database-First 조회
        var cached = await _repository.GetByBarcodeAsync(barcode);
        if (cached != null) 
        {
            _logger.LogInformation("데이터베이스 히트: {Barcode}", barcode);
            return cached;
        }
        
        // 외부 API 우선순위 조회
        var sortedProviders = _providers.OrderBy(p => p.Priority);
        foreach (var provider in sortedProviders)
        {
            var result = await provider.GetByBarcodeAsync(barcode);
            if (result != null)
            {
                _logger.LogInformation("외부 API 히트: {Provider}", provider.GetType().Name);
                await _repository.AddAsync(result);
                return result;
            }
        }
        
        throw new NotFoundException($"바코드 {barcode}에 해당하는 미디어를 찾을 수 없습니다.");
    }
}
```

#### 2. HttpClientFactory 패턴

```csharp
public class GoogleBooksProvider : IMediaProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleBooksProvider> _logger;
    private readonly IOptions<ExternalApiSettings> _settings;
    
    // HttpClientFactory가 관리하는 HttpClient 주입
    public GoogleBooksProvider(
        HttpClient httpClient,
        ILogger<GoogleBooksProvider> logger,
        IOptions<ExternalApiSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings;
        
        // API 키를 헤더에 추가
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.Value.GoogleBooksApiKey);
    }
    
    public int Priority => 1; // 도서 검색 1순위
    
    public async Task<MediaItem?> GetByBarcodeAsync(string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"volumes?q=isbn:{barcode}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Books API 실패: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            // JSON 파싱 및 Book 엔티티 생성...
            return ParseGoogleBooksResponse(json, barcode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Google Books API 호출 오류");
            return null;
        }
    }
}
```

### 미들웨어 파이프라인 (Middleware Pipeline)

#### 전역 예외 처리 미들웨어

```csharp
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
            await _next(context); // 다음 미들웨어 실행
        }
        catch (InvalidBarcodeException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "INVALID_BARCODE");
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound, "NOT_FOUND");
        }
        catch (RateLimitExceededException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status429TooManyRequests, "RATE_LIMIT_EXCEEDED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "처리되지 않은 예외 발생");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR");
        }
    }
    
    private static async Task HandleExceptionAsync(
        HttpContext context, 
        Exception exception, 
        int statusCode, 
        string errorCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        var errorResponse = new ErrorResponse
        {
            Code = errorCode,
            Message = exception.Message,
            Timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}
```

### Entity Framework Core 10 설정

#### DbContext 설정

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<MediaItem> MediaItems { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MusicAlbum> MusicAlbums { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Fluent API 설정을 별도 클래스로 분리
        modelBuilder.ApplyConfiguration(new MediaItemConfiguration());
        modelBuilder.ApplyConfiguration(new BookConfiguration());
        modelBuilder.ApplyConfiguration(new MovieConfiguration());
        modelBuilder.ApplyConfiguration(new MusicAlbumConfiguration());
    }
}
```

#### Fluent API Configuration

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Table Per Type (TPT) 전략
        builder.ToTable("books");
        
        // Primary Key는 MediaItem과 공유
        builder.HasKey(b => b.Id);
        
        // 외래 키 관계
        builder.HasOne(b => b.MediaItem)
               .WithOne()
               .HasForeignKey<Book>(b => b.Id);
        
        // PostgreSQL 배열 타입
        builder.Property(b => b.Authors)
               .HasColumnType("text[]")
               .IsRequired();
        
        // ISBN 인덱스
        builder.HasIndex(b => b.Isbn13)
               .IsUnique();
        
        // 필드 제약
        builder.Property(b => b.Title)
               .HasMaxLength(500)
               .IsRequired();
        
        builder.Property(b => b.Publisher)
               .HasMaxLength(255);
    }
}
```

### Configuration 관리 (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=collectiondb;Username=admin;Password=password"
  },
  
  "ExternalApis": {
    "GoogleBooks": {
      "BaseUrl": "https://www.googleapis.com/books/v1/",
      "ApiKey": "YOUR_API_KEY_HERE",
      "Timeout": 10,
      "Priority": 1
    },
    "TMDb": {
      "BaseUrl": "https://api.themoviedb.org/3/",
      "ApiKey": "YOUR_API_KEY_HERE",
      "Timeout": 10,
      "Priority": 1
    },
    "MusicBrainz": {
      "BaseUrl": "https://musicbrainz.org/ws/2/",
      "Timeout": 10,
      "Priority": 1,
      "RateLimit": 1
    }
  },
  
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowMinutes": 1,
    "QueueLimit": 10
  }
}
```

### ASP.NET Core 통합 테스트 (WebApplicationFactory)

```csharp
public class MediaEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public MediaEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetMediaByBarcode_ValidIsbn_ReturnsOk()
    {
        // Arrange
        var isbn = "9788932917245"; // 유효한 ISBN-13
        
        // Act
        var response = await _client.GetAsync($"/items/{isbn}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MediaItem>();
        content.Should().NotBeNull();
        content!.MediaType.Should().Be(MediaType.Book);
    }
}

// Test Factory (In-Memory DB)
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 기존 DbContext 제거
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            
            // In-Memory DB로 교체
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
            
            // DB 초기화
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
```

### ASP.NET Core 베스트 프랙티스 체크리스트

- [x] **Minimal API 사용**: 단순한 라우팅에 적합, Program.cs에 집중
- [x] **의존성 주입 활용**: 모든 서비스는 인터페이스로 추상화
- [x] **HttpClientFactory 사용**: HttpClient 재사용, 연결 풀 관리
- [x] **Options 패턴**: IOptions<T>로 강타입 설정 관리
- [x] **미들웨어 순서**: Rate Limiting → Logging → Error Handling
- [x] **EF Core Best Practices**: 
  - Fluent API로 엔티티 설정 분리
  - 비동기 쿼리 (ToListAsync, FirstOrDefaultAsync)
  - Compiled Queries로 성능 최적화
- [x] **로깅**: Serilog로 구조화된 로깅, LogInformation/LogWarning/LogError 구분
- [x] **OpenAPI/Swagger**: WithOpenApi()로 자동 문서 생성
- [x] **Health Checks**: /health 엔드포인트로 DB 연결 확인
- [x] **환경별 설정**: appsettings.{Environment}.json으로 분리
- [x] **User Secrets**: 로컬 개발 시 민감 정보 보호 (dotnet user-secrets)

---
