# Research: 미디어 정보 API 서버

**Feature**: 001-isbn-book-api  
**Phase**: 0 - Research & Technology Validation  
**Date**: 2025-11-16

## 목적

다양한 미디어 유형의 바코드/ISBN 조회 API 개발을 위한 기술 선택, 외부 API 통합 전략, Database-First 아키텍처 구현 방법을 연구하고 최적의 접근 방식을 결정합니다.

## 주요 연구 항목

### 1. ASP.NET Core 10.0 기술 스택

#### 결정사항
- **웹 프레임워크**: **ASP.NET Core 10.0** (Minimal APIs 패턴)
- **런타임**: .NET 10.0 (LTS)
- **언어**: C# 13
- **ORM**: Entity Framework Core 10.0
- **데이터베이스 드라이버**: Npgsql.EntityFrameworkCore.PostgreSQL 10.0

#### 근거
1. **ASP.NET Core 10.0 (웹 프레임워크)**:
   - **Minimal APIs**: 경량 HTTP API 개발에 최적화
     ```csharp
     app.MapGet("/items/{barcode}", async (string barcode, IMediaService service) 
         => await service.GetMediaByBarcodeAsync(barcode));
     ```
   - **내장 Rate Limiting**: AddRateLimiter() / UseRateLimiter() 미들웨어
   - **의존성 주입 (DI)**: IServiceCollection을 통한 강력한 DI 컨테이너
   - **미들웨어 파이프라인**: 요청/응답 처리 체인 (UseMiddleware<T>)
   - **Configuration 시스템**: appsettings.json, 환경 변수, User Secrets 통합
   - **OpenAPI 지원**: Swashbuckle.AspNetCore로 자동 API 문서 생성
   - **HttpClientFactory**: 효율적인 HTTP 클라이언트 관리

2. **.NET 10.0 LTS 런타임**: 
   - 2024년 11월 출시된 최신 안정 버전
   - 향상된 성능 (Native AOT 지원, JSON 직렬화 최적화)
   - 6년간의 장기 지원 (2030년까지)
   - C# 13의 최신 언어 기능 활용 가능
   - 크로스 플랫폼 (Linux, Windows, macOS)

3. **Minimal APIs vs Controller 기반 선택**:
   - **Minimal APIs 선택 이유**:
     - 단일 엔드포인트 API에 최적 (GET /items/{barcode})
     - Program.cs에 모든 로직 집중 → 간결성
     - 낮은 메모리 사용량 (Controller, View 불필요)
     - 빠른 시작 시간 (Reflection 오버헤드 감소)
   - **Controller 기반 대안 거부 이유**:
     - CRUD API나 복잡한 라우팅에 적합
     - 본 프로젝트는 조회 전용 단일 엔드포인트
     - Attribute Routing, Model Binding 등의 기능 불필요

4. **Entity Framework Core 10.0**:
   - Database-First 패턴 완벽 지원
   - 복잡한 쿼리 최적화 (Compiled Queries)
   - PostgreSQL 네이티브 기능 지원 (JSONB, 배열 타입)
   - 마이그레이션 도구로 스키마 버전 관리
   - AddDbContext<T>()로 DI 통합

#### ASP.NET Core 핵심 개념 활용
| 개념 | 사용 사례 | 구현 위치 |
|-----|---------|---------|
| **Minimal API** | 엔드포인트 정의 | Program.cs - MapGet() |
| **의존성 주입** | 서비스 등록/해결 | Program.cs - AddScoped/AddSingleton |
| **미들웨어** | 전역 예외 처리, Rate Limiting | ErrorHandlingMiddleware, UseRateLimiter() |
| **Options 패턴** | 강타입 설정 | IOptions<ExternalApiSettings> |
| **HttpClientFactory** | 외부 API 호출 | AddHttpClient<T>() |
| **Logging** | 구조화된 로깅 | ILogger<T>, Serilog.AspNetCore |
| **Configuration** | 환경별 설정 | appsettings.json, User Secrets |

#### 고려한 대안
- **.NET 8.0**: 안정적이지만 .NET 10.0의 성능 개선 및 최신 ASP.NET Core 기능 부족
- **Dapper (Micro ORM)**: 성능은 우수하나 EF Core 10의 변경 추적 및 마이그레이션 기능 필요
- **Controller 기반 MVC**: 복잡한 라우팅에 적합하지만 단일 엔드포인트에는 과도
- **FastEndpoints 라이브러리**: 서드파티 의존성 추가, ASP.NET Core 내장 Minimal API로 충분

---

### 2. 바코드 형식 감지 및 검증

#### 결정사항
**바코드 형식별 자동 감지 로직**:
```csharp
// ISBN-10: 10자리 숫자 (체크섬 포함)
// ISBN-13: 978/979로 시작하는 13자리 (도서)
// EAN-13: 13자리 숫자 (978/979 아닌 경우 영상/음악)
// UPC: 12자리 숫자 (영상/음악)
```

**검증 알고리즘**:
- ISBN-10: Modulo 11 체크섬
- ISBN-13/EAN-13: Modulo 10 체크섬
- UPC: Modulo 10 체크섬

#### 근거
1. **형식 우선 검증**: 잘못된 바코드 조기 거부로 외부 API 호출 최소화
2. **체크섬 검증**: 입력 오류 방지 (바코드 스캐너 오류, 수동 입력 실수)
3. **자동 타입 감지**: 클라이언트가 미디어 타입을 지정할 필요 없음

#### 구현 예제
```csharp
public class BarcodeValidator
{
    public BarcodeType DetectAndValidate(string barcode)
    {
        barcode = barcode.Replace("-", "").Trim();
        
        if (barcode.Length == 10 && ValidateIsbn10(barcode))
            return BarcodeType.ISBN10;
        
        if (barcode.Length == 13 && ValidateIsbn13(barcode))
        {
            if (barcode.StartsWith("978") || barcode.StartsWith("979"))
                return BarcodeType.ISBN13_Book;
            return BarcodeType.EAN13_Media;
        }
        
        if (barcode.Length == 12 && ValidateUpc(barcode))
            return BarcodeType.UPC_Media;
        
        throw new ArgumentException("Invalid barcode format");
    }
}
```

#### 고려한 대안
- **외부 라이브러리 (ZXing.Net)**: 바코드 이미지 스캔용이지만 텍스트 검증에는 과도
- **정규식만 사용**: 체크섬 검증 없이는 잘못된 입력 허용 가능성

---

### 3. Database-First 아키텍처

#### 결정사항
**2단계 조회 전략**:
1. **1단계**: PostgreSQL 데이터베이스에서 바코드 조회
2. **2단계**: 미발견 시 외부 API 호출 → 결과를 데이터베이스에 저장

**데이터베이스 스키마**:
```sql
-- 통합 미디어 테이블 (공통 필드)
CREATE TABLE media_items (
    id UUID PRIMARY KEY,
    barcode VARCHAR(13) UNIQUE NOT NULL,
    media_type VARCHAR(20) NOT NULL,  -- Book, Movie, Music
    title TEXT NOT NULL,
    description TEXT,
    image_url TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    INDEX idx_barcode (barcode)
);

-- 도서 특화 테이블
CREATE TABLE books (
    media_item_id UUID PRIMARY KEY REFERENCES media_items(id),
    isbn13 VARCHAR(13) NOT NULL,
    authors TEXT[] NOT NULL,
    publisher VARCHAR(255),
    publish_date DATE,
    page_count INT
);

-- 영화 특화 테이블 (Blu-ray/DVD)
CREATE TABLE movies (
    media_item_id UUID PRIMARY KEY REFERENCES media_items(id),
    director VARCHAR(255),
    cast TEXT[],
    runtime_minutes INT,
    release_date DATE,
    rating VARCHAR(10)
);

-- 음악 특화 테이블
CREATE TABLE music_albums (
    media_item_id UUID PRIMARY KEY REFERENCES media_items(id),
    artist VARCHAR(255) NOT NULL,
    tracks JSONB,  -- [{title, duration, track_number}]
    release_date DATE,
    label VARCHAR(255)
);
```

#### 근거
1. **성능**: 데이터베이스 조회 (<50ms) vs 외부 API 호출 (>1초)
2. **안정성**: 외부 API 다운타임에도 이전 조회 데이터 제공 가능
3. **비용 절감**: 반복 조회 시 외부 API 호출 없음 (무료 API 제한 회피)
4. **데이터 일관성**: 중앙 집중식 데이터 관리, 여러 소스의 데이터 병합 가능

#### EF Core 10.0 구현
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<MediaItem> MediaItems { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MusicAlbum> MusicAlbums { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TPT (Table Per Type) 전략
        modelBuilder.Entity<MediaItem>()
            .HasIndex(m => m.Barcode)
            .IsUnique();
        
        modelBuilder.Entity<Book>()
            .Property(b => b.Authors)
            .HasColumnType("text[]");  // PostgreSQL 배열
        
        modelBuilder.Entity<MusicAlbum>()
            .Property(m => m.Tracks)
            .HasColumnType("jsonb");  // PostgreSQL JSONB
    }
}
```

#### 고려한 대안
- **Cache-First (Redis)**: 영구 저장 없이 TTL 기반 캐싱 → 데이터 손실 가능
- **API-First (Database는 보조)**: 항상 외부 API 조회 → 느리고 비용 증가

---

### 4. 외부 API 통합 및 우선순위 전략

#### 결정사항
**미디어 타입별 우선순위**:

| 미디어 타입 | 1순위 | 2순위 | 3순위 |
|------------|-------|-------|-------|
| 도서 (Book) | Google Books API | Kakao Book Search API | Aladin API |
| 영화 (Blu-ray/DVD) | TMDb (The Movie Database) | OMDb (Open Movie Database) | - |
| 음악 (Album) | MusicBrainz API | Discogs API | - |

**폴백 전략**:
1. 1순위 API 호출
2. 실패 또는 불완전한 데이터 시 2순위 시도
3. 모든 소스 실패 시 404 Not Found 반환

#### 근거
1. **Google Books API (도서 1순위)**:
   - 가장 포괄적인 도서 데이터베이스
   - 표지 이미지 고품질
   - 무료 API (할당량: 1000 req/day)

2. **TMDb (영화 1순위)**:
   - 방대한 영화/TV 데이터베이스
   - 커뮤니티 기반 데이터 (정확도 높음)
   - 무료 API 키 (할당량: 충분)

3. **MusicBrainz (음악 1순위)**:
   - 오픈 소스 음악 데이터베이스
   - 트랙 정보 상세
   - 무료 사용 (Rate Limit: 1 req/sec)

#### 구현 패턴
```csharp
public interface IMediaProvider
{
    Task<MediaItem?> GetByBarcodeAsync(string barcode);
    int Priority { get; }  // 우선순위 (낮을수록 높은 우선순위)
}

public class MediaService : IMediaService
{
    private readonly IEnumerable<IMediaProvider> _providers;
    private readonly IMediaRepository _repository;
    
    public async Task<MediaItem> GetMediaByBarcodeAsync(string barcode)
    {
        // 1. Database-First 조회
        var cached = await _repository.GetByBarcodeAsync(barcode);
        if (cached != null) return cached;
        
        // 2. 우선순위에 따라 외부 API 시도
        var sortedProviders = _providers.OrderBy(p => p.Priority);
        foreach (var provider in sortedProviders)
        {
            var result = await provider.GetByBarcodeAsync(barcode);
            if (result != null)
            {
                // 3. 데이터베이스에 저장
                await _repository.AddAsync(result);
                return result;
            }
        }
        
        throw new NotFoundException($"Media not found for barcode: {barcode}");
    }
}
```

#### HttpClientFactory 사용
```csharp
// Program.cs
builder.Services.AddHttpClient<GoogleBooksProvider>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<TMDbProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

#### 고려한 대안
- **병렬 호출 (모든 API 동시 호출)**: 불필요한 API 호출 증가, Rate Limit 위반 위험
- **단일 소스만 사용**: 해당 API 다운타임 시 서비스 불가

---

### 5. 오류 처리 및 HTTP 상태 코드

#### 결정사항
**표준 HTTP 상태 코드**:
- `200 OK`: 미디어 정보 성공적으로 반환
- `400 Bad Request`: 잘못된 바코드 형식
- `404 Not Found`: 유효한 바코드이지만 미디어 미발견
- `429 Too Many Requests`: 속도 제한 초과
- `500 Internal Server Error`: 서버 내부 오류
- `503 Service Unavailable`: 모든 외부 API 다운

**오류 응답 형식**:
```json
{
  "error": {
    "code": "INVALID_BARCODE_FORMAT",
    "message": "The provided barcode format is invalid. Expected ISBN-10, ISBN-13, UPC, or EAN-13.",
    "details": {
      "provided": "12345",
      "expected_formats": ["ISBN-10 (10 digits)", "ISBN-13 (13 digits)", "UPC (12 digits)", "EAN-13 (13 digits)"]
    }
  }
}
```

#### 구현: 전역 예외 처리 미들웨어
```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "INVALID_BARCODE_FORMAT");
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound, "MEDIA_NOT_FOUND");
        }
        catch (RateLimitExceededException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status429TooManyRequests, "RATE_LIMIT_EXCEEDED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR");
        }
    }
}
```

#### 근거
- **명확한 오류 메시지**: 클라이언트가 문제를 이해하고 수정 가능
- **표준 HTTP 상태 코드**: RESTful API 모범 사례
- **구조화된 오류 응답**: 프로그래밍 방식으로 처리 가능

---

### 6. 속도 제한 (Rate Limiting)

#### 결정사항
**ASP.NET Core 10.0 내장 Rate Limiting 미들웨어 사용**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 100;           // 100 requests
        options.Window = TimeSpan.FromMinutes(1);  // per minute
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });
});

app.UseRateLimiter();

app.MapGet("/items/{barcode}", async (string barcode, IMediaService service) 
    => await service.GetMediaByBarcodeAsync(barcode))
    .RequireRateLimiting("fixed");
```

#### 근거
- **.NET 10.0 네이티브 지원**: 외부 라이브러리 불필요
- **외부 API 보호**: 과도한 요청으로 인한 외부 API 차단 방지
- **서버 자원 보호**: DoS 공격 방지

#### 고려한 대안
- **AspNetCoreRateLimit 라이브러리**: 기능은 풍부하지만 .NET 10 내장 기능으로 충분
- **Redis 기반 분산 Rate Limiting**: 단일 서버 환경에서는 불필요

---

### 7. 로깅 및 모니터링

#### 결정사항
**Serilog.AspNetCore 10.0**:
```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CollectionServer")
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File("logs/api-.log", 
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}");
});
```

**로깅 전략**:
- **Info**: API 요청/응답 (바코드, 미디어 타입, 응답 시간)
- **Warning**: 외부 API 폴백, 느린 쿼리
- **Error**: 예외, 외부 API 실패

#### 근거
- **구조화된 로깅**: JSON 형식으로 분석 및 검색 용이
- **성능 추적**: 데이터베이스 vs 외부 API 응답 시간 비교
- **디버깅**: 외부 API 실패 원인 파악

---

### 8. 배포 및 컨테이너화

#### 결정사항
**Podman 컨테이너**:
```dockerfile
# Containerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CollectionServer.Api/CollectionServer.Api.csproj", "src/CollectionServer.Api/"]
COPY ["src/CollectionServer.Core/CollectionServer.Core.csproj", "src/CollectionServer.Core/"]
COPY ["src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj", "src/CollectionServer.Infrastructure/"]
RUN dotnet restore "src/CollectionServer.Api/CollectionServer.Api.csproj"

COPY . .
WORKDIR "/src/src/CollectionServer.Api"
RUN dotnet build "CollectionServer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CollectionServer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CollectionServer.Api.dll"]
```

**Podman Compose**:
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: collectiondb
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
  
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=collectiondb;Username=admin;Password=${DB_PASSWORD}
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - postgres

volumes:
  postgres_data:
```

#### 근거
- **이식성**: 로컬 개발, CI/CD, 프로덕션 환경 일관성
- **격리**: PostgreSQL과 API 분리
- **스케일링**: Kubernetes 배포 용이
- **보안**: Rootless 컨테이너 실행 가능
- **호환성**: OCI 표준 준수로 Docker 이미지와 호환

---

## 연구 결과 요약

### 확정된 기술 스택

| 구성 요소 | 선택 | 버전 | 역할 |
|----------|------|------|------|
| 웹 프레임워크 | **ASP.NET Core** | 10.0 | HTTP API, 미들웨어, DI |
| 런타임 | .NET | 10.0 | LTS, 크로스 플랫폼 |
| 언어 | C# | 13 | 최신 언어 기능 |
| ORM | Entity Framework Core | 10.0 | PostgreSQL 통합 |
| 데이터베이스 | PostgreSQL | 16+ | 주 데이터 저장소 |
| 데이터베이스 드라이버 | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0 | PostgreSQL Provider |
| API 문서화 | Swashbuckle.AspNetCore | 7.0.0 | OpenAPI/Swagger |
| 로깅 | Serilog.AspNetCore | 10.0 | 구조화된 로깅 |
| 테스팅 | xUnit + Moq + FluentAssertions | 최신 | 단위/통합 테스트 |
| 컨테이너 | Podman | 최신 | OCI 컨테이너 |

### ASP.NET Core 관련 NuGet 패키지

```xml
<!-- CollectionServer.Api.csproj - ASP.NET Core Web API -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core 핵심 패키지 -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />
    
    <!-- Rate Limiting (ASP.NET Core 10 내장) -->
    <PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="10.0.0" />
    
    <!-- Logging -->
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Formatting.Compact" Version="3.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- 프로젝트 참조 -->
    <ProjectReference Include="../CollectionServer.Core/CollectionServer.Core.csproj" />
    <ProjectReference Include="../CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj" />
  </ItemGroup>
</Project>

<!-- CollectionServer.Core.csproj - 도메인 레이어 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core 추상화 (ILogger 등) -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.0" />
  </ItemGroup>
</Project>

<!-- CollectionServer.Infrastructure.csproj - 인프라 레이어 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Entity Framework Core + PostgreSQL -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    
    <!-- HttpClientFactory (ASP.NET Core) -->
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
    
    <!-- Options 패턴 -->
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../CollectionServer.Core/CollectionServer.Core.csproj" />
  </ItemGroup>
</Project>

<!-- CollectionServer.UnitTests.csproj - 단위 테스트 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/CollectionServer.Core/CollectionServer.Core.csproj" />
    <ProjectReference Include="../../src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj" />
  </ItemGroup>
</Project>

<!-- CollectionServer.IntegrationTests.csproj - 통합 테스트 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core 통합 테스트 -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
    
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/CollectionServer.Api/CollectionServer.Api.csproj" />
  </ItemGroup>
</Project>
```

### global.json

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

### 시스템 요구사항

- **.NET 10 SDK**: 개발 및 빌드
- **PostgreSQL 16+**: 프로덕션 데이터베이스
- **Podman**: 컨테이너 배포 (선택 사항)

### 핵심 아키텍처 결정

1. **Database-First**: 내부 DB 우선 조회 → 외부 API 폴백
2. **우선순위 기반 외부 API**: 미디어 타입별 최적 소스 선택
3. **Clean Architecture**: API, Core, Infrastructure 레이어 분리
4. **ASP.NET Core Minimal APIs**: 간결한 엔드포인트 정의 (vs Controller 기반)
5. **의존성 주입 (DI)**: ASP.NET Core DI Container로 모든 서비스 관리
6. **미들웨어 파이프라인**: 전역 예외 처리, Rate Limiting, 로깅
7. **HttpClientFactory**: 외부 API 호출 효율성 및 연결 풀 관리
8. **Options 패턴**: IOptions<T>로 강타입 설정 관리

### ASP.NET Core 10 최신 기능 활용

#### 1. Minimal APIs (vs Controller)
```csharp
// ✅ Minimal API (선택) - 단일 엔드포인트 최적화
app.MapGet("/items/{barcode}", async (string barcode, IMediaService service) 
    => await service.GetMediaByBarcodeAsync(barcode))
    .RequireRateLimiting("api")
    .WithOpenApi();

// ❌ Controller 기반 (거부) - 불필요한 복잡성
[ApiController]
[Route("items")]
public class MediaController : ControllerBase { ... }
```

#### 2. 내장 Rate Limiting (ASP.NET Core 7+)
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

#### 3. OpenAPI 자동 생성
```csharp
app.MapGet("/items/{barcode}", ...)
    .WithName("GetMediaByBarcode")
    .WithOpenApi(op => new(op)
    {
        Summary = "바코드로 미디어 조회",
        Description = "ISBN-10/13, UPC, EAN-13 지원"
    })
    .Produces<MediaItem>(200)
    .Produces<ErrorResponse>(400)
    .Produces<ErrorResponse>(404);
```

#### 4. 결과 타입 (IResult)
```csharp
// TypedResults 사용 (강타입 반환)
app.MapGet("/items/{barcode}", async (string barcode, IMediaService service) =>
{
    try
    {
        var media = await service.GetMediaByBarcodeAsync(barcode);
        return TypedResults.Ok(media);
    }
    catch (NotFoundException)
    {
        return TypedResults.NotFound(new { error = "미디어를 찾을 수 없습니다." });
    }
});
```

#### 5. 엔드포인트 필터 (Endpoint Filters)
```csharp
// 요청 검증 필터
app.MapGet("/items/{barcode}", ...)
    .AddEndpointFilter(async (context, next) =>
    {
        var barcode = context.GetArgument<string>(0);
        if (string.IsNullOrWhiteSpace(barcode))
            return Results.BadRequest("바코드가 비어있습니다.");
        
        return await next(context);
    });
```

### ASP.NET Core 개발 도구

| 도구 | 용도 | 명령어 |
|-----|------|-------|
| **dotnet CLI** | 프로젝트 관리 | `dotnet new webapi`, `dotnet run`, `dotnet build` |
| **EF Core CLI** | 마이그레이션 | `dotnet ef migrations add`, `dotnet ef database update` |
| **User Secrets** | 로컬 개발 비밀 | `dotnet user-secrets set "ApiKey" "value"` |
| **dotnet watch** | Hot Reload | `dotnet watch run` (코드 변경 시 자동 재시작) |
| **Swagger UI** | API 테스트 | https://localhost:5001/swagger |

### 다음 단계 (Phase 1)

1. **data-model.md**: 엔티티 상세 설계 (필드, 관계, 제약 조건)
2. **contracts/**: OpenAPI 스키마 정의 (Swagger 기반)
3. **quickstart.md**: 개발 환경 설정 가이드 (ASP.NET Core 10 설치, PostgreSQL 설정)

---

**연구 완료**: 모든 NEEDS CLARIFICATION 항목 해결 완료 ✅  
**ASP.NET Core 10 특화**: 웹 프레임워크 핵심 기능 명시 완료 ✅
