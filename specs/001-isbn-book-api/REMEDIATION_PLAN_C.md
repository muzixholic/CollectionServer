# Plan C - 전체 27개 이슈 수정 계획

**생성일**: 2025-11-18  
**상태**: 진행 중 (2/27 완료)  
**예상 소요 시간**: 10-12시간

---

## ✅ 완료된 수정 (2/27)

### C1: 헌장 Principle II 위반 - 기술 스택 버전 모호성
- **파일**: `spec.md` Line 7
- **변경**: "ASP.NET Core 최신 버전" → "ASP.NET Core 10.0"
- **상태**: ✅ 완료

### C2: 헌장 Principle II - EF Core 버전 정책 공백
- **파일**: `constitution.md` Lines 61-66
- **변경**: "ORM: Entity Framework Core (프레임워크 버전과 일치하는 메이저 버전 사용)" 추가
- **상태**: ✅ 완료

---

## 🔄 진행 중 수정 (3/27)

### I1: 바코드 유형 명명 불일치
- **파일**: `spec.md` Line 153 (FR-001)
- **변경**: "UPC-12" → "UPC"
- **상태**: ✅ 완료

### A3: 바코드 미디어 유형 감지 로직 모호
- **파일**: `spec.md` Lines 155-164 (FR-003)
- **변경**: UPC/EAN-13 감지 전략 상세화 (TMDb → MusicBrainz 순차 시도)
- **상태**: ✅ 완료

### I2: 미디어 타입 명명 불일치 - 용어집 추가
- **파일**: `spec.md` Line 245 이후
- **변경**: 용어 정의 섹션 추가 (도서, 영화, 음악 앨범, 바코드 정의)
- **상태**: ✅ 완료

---

## 📋 남은 수정 작업 (22/27)

### HIGH Priority (6개)

#### I3: "Aladin API" 명명 일관성
**위치**: `spec.md` 전체, `tasks.md` T082  
**변경 내용**:
- [ ] spec.md: "Aladin API" → "Aladin 도서 검색 API" 통일
- [ ] tasks.md T082: "AladinApiProvider.cs" → "AladinProvider.cs"

#### I4: Provider 파일명 불일치
**위치**: `tasks.md` T080-T086  
**변경 내용**:
- [ ] 모든 Provider 파일명을 `*Provider.cs` 패턴으로 통일
  - GoogleBooksProvider.cs ✅
  - KakaoBookProvider.cs ✅
  - AladinProvider.cs (변경 필요)
  - TMDbProvider.cs ✅
  - OMDbProvider.cs ✅
  - MusicBrainzProvider.cs ✅
  - DiscogsProvider.cs ✅

#### I5: API 베이스 경로 미명시
**위치**: `spec.md`, `plan.md`, `contracts/openapi.yaml`  
**변경 내용**:
- [ ] spec.md FR-001 이전에 "API 엔드포인트 구조" 섹션 추가:
```markdown
### API 엔드포인트 구조

**베이스 URL**: 
- 개발: `http://localhost:5000`
- 프로덕션: `https://api.example.com`

**API 버전**: 버전 경로 없음 (v1은 기본, 향후 v2 추가 시 /v2 prefix 사용)

**엔드포인트 형식**: `GET /items/{barcode}`

**예시**:
- 개발: `http://localhost:5000/items/9788932917245`
- 프로덕션: `https://api.example.com/items/9788932917245`
```

- [ ] contracts/openapi.yaml에 servers 섹션 추가:
```yaml
servers:
  - url: http://localhost:5000
    description: 개발 환경
  - url: https://api.example.com
    description: 프로덕션 환경
```

#### U1: 성공 기준 SC-001 측정 방법 미명시
**위치**: `spec.md` Line 222  
**변경 내용**:
```markdown
- **SC-001**: API는 통합 테스트 스위트의 표준 바코드 샘플 100개 중 최소 95개에 대해 완전한 미디어 정보를 반환합니다
  - **측정 방법**: `tests/CollectionServer.IntegrationTests/Data/StandardBarcodes.json`에 정의된 검증 세트 사용
  - **샘플 구성**: 도서 30개, 영화 30개, 음악 앨범 30개, 엣지 케이스 10개
  - **완전한 정보 정의**: 제목, 주요 메타데이터(저자/감독/아티스트), 발매일이 모두 존재
  - **조건**: 외부 API 정상 작동 시 측정
```

- [ ] tasks.md Phase 3에 작업 추가:
```markdown
- [ ] T070.1 [US1] tests/CollectionServer.IntegrationTests/Data/StandardBarcodes.json 생성 (100개 검증용 바코드 큐레이션)
```

#### U2: 성공 기준 SC-003 "100% 정확도" 비현실적
**위치**: `spec.md` Line 224  
**변경 내용**:
```markdown
- **SC-003**: API는 표준 체크섬 알고리즘(ISBN-10/13 Modulo 10/13, UPC/EAN Luhn)에 따라 바코드 형식을 검증하고, 알려진 유효 바코드 테스트 셋에서 99.9% 이상의 정확도로 미디어 유형을 감지합니다
  - **측정 방법**: 단위 테스트에서 각 바코드 유형별 100개 샘플 검증
  - **허용 오차**: 경계 케이스(체크섬 변경, 비표준 형식) 0.1% 미만
  - **검증 범위**: 유효성 검증 정확도 + 미디어 유형 감지 정확도
```

#### U3: Rate Limit 식별자 미명시
**위치**: `spec.md` Lines 213-215 (FR-025), `plan.md` Line 339  
**변경 내용**:

**spec.md FR-025 수정**:
```markdown
- **FR-025**: 시스템은 API 악용을 방지하기 위해 클라이언트 IP 주소 기반 속도 제한을 구현해야 합니다
  - **정책**: 고정 윈도우 (Fixed Window) 방식
  - **제한**: IP당 분당 100개 요청
  - **큐**: 초과 요청 10개까지 대기열 허용
  - **식별**: 클라이언트 IP 주소 (X-Forwarded-For 헤더 우선, 없으면 Connection.RemoteIpAddress)
  - **향후 확장**: API 키 기반 티어별 제한 (Phase 2 이후)
```

**plan.md Program.cs 구성 업데이트**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // IP 주소 기반 식별자
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
});
```

#### A1: "합리적인 시간" 모호
**위치**: `spec.md` Line 238  
**변경 내용**:
```markdown
Database-First 아키텍처와 캐싱으로 인해 응답이 다음 목표 시간 내에 도착합니다:
- 데이터베이스 조회: 500ms 미만 (P99)
- 외부 API 조회: 2초 미만 (P99)
- Health Check: 100ms 미만
```

#### A2: "불완전한 데이터" 정의 미명시
**위치**: `spec.md` Line 194 (FR-014), Line 198 (FR-019)  
**변경 내용**:

**FR-019 명확화**:
```markdown
- **FR-019**: 시스템은 누락되거나 불완전한 데이터를 우아하게 처리해야 합니다
  - **필수 필드**: 제목(Title), 바코드(Barcode), 미디어 타입(MediaType), 발매일(ReleaseDate)
  - **선택 필드**: 설명, 이미지 URL, 상세 메타데이터 (저자, 감독, 트랙 목록 등)
  - **처리 방식**: 
    - 필수 필드 누락 시: 해당 소스를 무효로 간주, 다음 우선순위 소스로 폴백
    - 선택 필드 누락 시: null 반환하고 부분 데이터로 저장
  - **검증**: 외부 API 응답 파싱 후 필수 필드 존재 여부 확인
```

**data-model.md에 엔티티 필드 제약 추가** (Phase 1 작업):
```markdown
## Book 엔티티 필드 제약

| 필드 | 타입 | 필수 | 기본값 | 설명 |
|------|------|------|--------|------|
| Id | Guid | ✅ | Auto | 기본 키 |
| Barcode | string(20) | ✅ | - | 정규화된 바코드 (UNIQUE INDEX) |
| MediaType | int | ✅ | 0 | 0=Book |
| Title | string(500) | ✅ | - | 도서 제목 |
| Authors | string[] | ✅ | [] | 저자 배열 (최소 1개) |
| Publisher | string(255) | ❌ | null | 출판사 |
| PublishedDate | date | ✅ | - | 출판일 |
| Description | text | ❌ | null | 설명 |
| CoverImageUrl | text | ❌ | null | 표지 URL |
| Isbn13 | string(13) | ✅ | - | 정규화된 ISBN-13 (UNIQUE INDEX) |
| PageCount | int | ❌ | null | 페이지 수 |
| Language | string(10) | ❌ | null | 언어 코드 (예: ko, en) |
| Categories | string[] | ❌ | [] | 카테고리 배열 |
| CreatedAt | timestamp | ✅ | NOW() | 생성 시각 |
| UpdatedAt | timestamp | ✅ | NOW() | 수정 시각 |
```

---

### MEDIUM Priority (14개)

#### I6: data-model.md 작성
**위치**: `plan.md` Line 116, `tasks.md` Phase 1  
**변경 내용**:
- [ ] tasks.md Phase 1에 우선순위 작업 추가:
```markdown
- [ ] T016.1 [P] specs/001-isbn-book-api/data-model.md 생성 (엔티티 ERD, 필드 정의, 제약 조건)
```

- [ ] `data-model.md` 생성 (전체 구조는 별도 문서 참조)

#### I7: NuGet 패키지 버전 실현 가능성
**위치**: `plan.md` Line 19, `tasks.md` T011  
**변경 내용**:

**plan.md 기술 컨텍스트 수정**:
```markdown
**주요 의존성 (Primary Dependencies)**:  
  - ASP.NET Core 10.0 (웹 프레임워크, 미들웨어 파이프라인)
  - Entity Framework Core 10.0 (ORM, 데이터 액세스)
  - Npgsql.EntityFrameworkCore.PostgreSQL (EF Core 10 호환 버전)
    - **참고**: .NET 10은 2025년 11월 정식 출시 예정. 개발 시점에 실제 사용 가능한 버전으로 대체.
  - Serilog.AspNetCore (최신 안정 버전)
  - Swashbuckle.AspNetCore 7.0+ (OpenAPI/Swagger 문서화)

**버전 정책**:
- 프로젝트 생성 시점(2025-11)에는 .NET 10 Preview 사용 가능
- 안정 버전 출시 후 업데이트 필요
- 패키지 버전은 NuGet에서 실제 사용 가능한 최신 버전 사용
```

**tasks.md T011 수정**:
```markdown
- [X] T011 [P] Infrastructure 프로젝트에 EF Core 패키지 추가
  - Microsoft.EntityFrameworkCore (실제 사용 가능한 최신 버전)
  - Npgsql.EntityFrameworkCore.PostgreSQL (EF Core 버전 호환)
  - Microsoft.EntityFrameworkCore.Design (마이그레이션 도구)
  - **참고**: dotnet add package 실행 시 --version 옵션으로 명시적 버전 지정
```

#### I8: Phase 4 vs Phase 7 우선순위 작업 중복
**위치**: `tasks.md` Lines 183-193 (Phase 4), Lines 271-296 (Phase 7)  
**변경 내용**:

**Phase 4 목표 명확화**:
```markdown
## Phase 4: 사용자 스토리 2 - 최종 사용자의 미디어 발견 (우선순위: P1)

**목표**: 외부 API 통합 및 **기본 우선순위 폴백** 구현. 각 미디어 유형별 1순위 Provider 성공 시 즉시 반환.

**범위**:
- 7개 외부 API Provider 구현
- 각 Provider에 Priority 속성 추가
- MediaService에 기본 우선순위 정렬 로직 추가 (OrderBy Priority)
- 1순위 Provider 실패 시 2순위로 폴백 (단순 루프)
```

**Phase 7 목표 명확화**:
```markdown
## Phase 7: 사용자 스토리 5 - 외부 데이터 소스 우선순위 및 폴백 (우선순위: P1)

**목표**: **고급 폴백 전략** 강화. 멀티소스 실패 처리, 상세 로깅, 폴백 메트릭 수집.

**범위**:
- 모든 우선순위 소스를 순차 시도하는 완전한 폴백 루프
- 각 소스 실패 시 상세 로깅 (어떤 소스 시도했는지)
- 폴백 통계 수집 (선택)
- 데이터 완전성 비교 로직 (선택, Phase 9 이후 고려)
```

**Phase 7 작업 수정**:
```markdown
- [ ] T136 [P] [US5] tests/CollectionServer.UnitTests/Services/MultiSourceFallbackTests.cs 생성 (2-3순위 폴백 시나리오 테스트)
- [ ] T137 [P] [US5] tests/CollectionServer.UnitTests/Services/FallbackMetricsTests.cs 생성 (폴백 통계 테스트)
- [ ] T139 [US5] MediaService 폴백 루프 개선 (모든 우선순위 소스 시도, 현재 Phase 4에서 기본 구현됨)
- [ ] T141 [US5] MediaService에 폴백 실패 시 상세 로깅 추가 (시도한 소스 목록, 각 실패 이유)
- [ ] T142 [US5] 각 Provider에 GetProviderName() 메서드 추가 (로깅용)
- [ ] T143 [US5] 폴백 통계 수집 서비스 추가 (선택, IFallbackMetrics 인터페이스)
```

#### U4: Database-First 동시성 제어 메커니즘 미명시
**위치**: `spec.md` Line 165 (FR-010), `tasks.md` T127  
**변경 내용**:

**FR-010 명확화**:
```markdown
- **FR-010**: 시스템은 동일한 바코드에 대한 동시 요청 시 중복 외부 API 호출 및 데이터베이스 저장을 방지해야 합니다
  - **메커니즘**: 바코드별 SemaphoreSlim(1,1) 잠금
  - **동작**: 동일 바코드 동시 요청 시 첫 요청만 외부 API 호출, 나머지는 대기 후 DB에서 조회
  - **타임아웃**: 외부 API 대기 최대 10초
  - **해제**: 요청 완료 시 자동 해제 (finally 블록)
```

**plan.md MediaService 구현 예시 추가**:
```csharp
public class MediaService : IMediaService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _barcodeLocks = new();
    private readonly IMediaRepository _repository;
    
    public async Task<MediaItem> GetMediaByBarcodeAsync(string barcode)
    {
        // 바코드별 잠금 획득
        var semaphore = _barcodeLocks.GetOrAdd(barcode, _ => new SemaphoreSlim(1, 1));
        
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(10));
        if (!acquired)
        {
            throw new TimeoutException($"바코드 {barcode} 처리 대기 시간 초과");
        }
        
        try
        {
            // Database-First 조회 (동시 요청은 여기서 DB 히트)
            var cached = await _repository.GetByBarcodeAsync(barcode);
            if (cached != null) 
            {
                _logger.LogInformation("데이터베이스 히트: {Barcode}", barcode);
                return cached;
            }
            
            // 외부 API 조회 (첫 요청만 실행됨)
            var result = await FetchFromExternalApis(barcode);
            await _repository.AddAsync(result);
            
            return result;
        }
        finally
        {
            semaphore.Release();
            
            // 선택: 일정 시간 후 SemaphoreSlim 정리 (메모리 누수 방지)
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
            {
                _barcodeLocks.TryRemove(barcode, out _);
            });
        }
    }
}
```

#### U5: Track 엔티티 구조 미명시
**위치**: `spec.md` Line 191 (FR-018), `plan.md` Line 73, `tasks.md` T023  
**변경 내용**:

**data-model.md에 Track 정의 추가**:
```markdown
### Track (값 객체 - Value Object)
- **설명**: 음악 앨범의 개별 트랙 정보. MusicAlbum 엔티티에 JSON 배열로 저장.
- **PostgreSQL 저장**: `tracks` 컬럼 (jsonb 타입)

| 필드 | 타입 | Nullable | 기본값 | 설명 |
|------|------|----------|--------|------|
| Position | int | ❌ | - | 트랙 순서 (1부터 시작) |
| Title | string(500) | ❌ | - | 트랙 제목 |
| DurationSeconds | int | ✅ | null | 재생 시간 (초) |
| DiscNumber | int | ❌ | 1 | 디스크 번호 (기본 1, 멀티 디스크용) |
| Artist | string(255) | ✅ | null | 트랙별 아티스트 (컴필레이션 앨범용) |

**C# 클래스 정의**:
```csharp
public class Track
{
    public int Position { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? DurationSeconds { get; init; }
    public int DiscNumber { get; init; } = 1;
    public string? Artist { get; init; }
    
    // Value object equality
    public override bool Equals(object? obj) => 
        obj is Track track && Position == track.Position && DiscNumber == track.DiscNumber;
    public override int GetHashCode() => HashCode.Combine(Position, DiscNumber);
}
```

**EF Core Configuration**:
```csharp
builder.Property(m => m.Tracks)
       .HasColumnType("jsonb")
       .HasConversion(
           v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
           v => JsonSerializer.Deserialize<List<Track>>(v, (JsonSerializerOptions)null));
```
```

#### U6: EF Core 마이그레이션 관리 전략 미명시
**위치**: `tasks.md` T035-T036, `plan.md` Line 85  
**변경 내용**:

**quickstart.md에 마이그레이션 섹션 추가**:
```markdown
## 데이터베이스 마이그레이션 관리

### 초기 마이그레이션 생성 및 적용

```bash
cd src/CollectionServer.Infrastructure

# 1. 초기 마이그레이션 생성
dotnet ef migrations add InitialCreate --startup-project ../CollectionServer.Api

# 2. 생성된 마이그레이션 SQL 검토
dotnet ef migrations script --startup-project ../CollectionServer.Api

# 3. 데이터베이스에 적용
dotnet ef database update --startup-project ../CollectionServer.Api
```

### 스키마 변경 시 새 마이그레이션 추가

```bash
# 1. 엔티티 수정 (예: Book에 Subtitle 필드 추가)
# Book.cs에 public string? Subtitle { get; set; } 추가

# 2. 마이그레이션 생성
dotnet ef migrations add AddBookSubtitle --startup-project ../CollectionServer.Api

# 3. 마이그레이션 SQL 검토 (프로덕션 배포 전 필수)
dotnet ef migrations script AddBookSubtitle --startup-project ../CollectionServer.Api

# 4. 적용
dotnet ef database update --startup-project ../CollectionServer.Api
```

### 프로덕션 배포 전략

#### 옵션 A: 자동 마이그레이션 (권장하지 않음)
```csharp
// Program.cs에 추가 (개발 환경만)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

#### 옵션 B: SQL 스크립트 생성 (권장)
```bash
# Idempotent 스크립트 생성 (반복 실행 안전)
dotnet ef migrations script --idempotent --output migration.sql --startup-project ../CollectionServer.Api

# DBA가 프로덕션 DB에 수동 실행
psql -h production-db.example.com -U admin -d collectiondb -f migration.sql
```

#### 옵션 C: CI/CD 파이프라인
```yaml
# .github/workflows/deploy.yml 예시
- name: Apply Migrations
  run: |
    dotnet ef database update --startup-project src/CollectionServer.Api --connection "${{ secrets.DB_CONNECTION_STRING }}"
```

### 마이그레이션 롤백

```bash
# 이전 마이그레이션으로 롤백
dotnet ef database update PreviousMigrationName --startup-project ../CollectionServer.Api

# 모든 마이그레이션 제거 (주의!)
dotnet ef database update 0 --startup-project ../CollectionServer.Api

# 마이그레이션 파일 삭제
dotnet ef migrations remove --startup-project ../CollectionServer.Api
```

### 베스트 프랙티스

1. **마이그레이션 이름**: 명확한 의미 전달 (예: AddBookSubtitle, AddIndexOnBarcode)
2. **SQL 검토**: 프로덕션 배포 전 반드시 `migrations script` 실행하여 SQL 확인
3. **Git 커밋**: 마이그레이션 파일은 반드시 Git에 커밋
4. **데이터 마이그레이션**: 데이터 변환이 필요한 경우 `migrationBuilder.Sql()` 사용
5. **롤백 계획**: 각 마이그레이션의 Down() 메서드 검증

### 트러블슈팅

**오류: "The migration '...' has already been applied to the database"**
```bash
# 마이그레이션 히스토리 확인
dotnet ef migrations list --startup-project ../CollectionServer.Api

# 데이터베이스 직접 확인
psql -d collectiondb -c "SELECT * FROM __EFMigrationsHistory;"
```

**오류: "No DbContext was found"**
- ApplicationDbContext가 Api 프로젝트에 DI 등록되었는지 확인
- --startup-project 옵션이 올바른지 확인
```
```

#### U7: 컨테이너 Health Check 기준 미명시
**위치**: `spec.md` A-010, `tasks.md` T182  
**변경 내용**:

**Containerfile에 Healthcheck 추가**:
```dockerfile
# Containerfile 마지막에 추가
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1
```

**Program.cs /health 엔드포인트 개선**:
```csharp
app.MapGet("/health", async (
    ApplicationDbContext dbContext,
    ILogger<Program> logger) =>
{
    var healthStatus = new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        service = "CollectionServer API",
        version = "1.0.0",
        checks = new Dictionary<string, object>()
    };
    
    // 1. 데이터베이스 연결 확인
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        var responseTime = await MeasureDbResponseTime(dbContext);
        
        healthStatus.checks["database"] = new
        {
            status = canConnect ? "Healthy" : "Unhealthy",
            responseTimeMs = responseTime,
            threshold = "< 500ms"
        };
        
        if (!canConnect || responseTime > 500)
        {
            logger.LogWarning("데이터베이스 Health Check 실패: canConnect={CanConnect}, responseTime={ResponseTime}ms", 
                canConnect, responseTime);
            return Results.ServiceUnavailable();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "데이터베이스 Health Check 예외");
        healthStatus.checks["database"] = new { status = "Unhealthy", error = ex.Message };
        return Results.ServiceUnavailable();
    }
    
    // 2. 메모리 사용량 (선택)
    var memoryUsedMb = GC.GetTotalMemory(false) / 1024 / 1024;
    healthStatus.checks["memory"] = new { usedMb = memoryUsedMb };
    
    return Results.Ok(healthStatus);
})
.WithName("HealthCheck")
.WithTags("Monitoring")
.WithOpenApi(operation => new(operation)
{
    Summary = "서비스 헬스 체크",
    Description = "데이터베이스 연결 상태와 서비스 가용성을 확인합니다."
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

static async Task<long> MeasureDbResponseTime(ApplicationDbContext dbContext)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
    sw.Stop();
    return sw.ElapsedMilliseconds;
}
```

#### U8: 엣지 케이스 구현 작업 매핑 누락
**위치**: `spec.md` Lines 115-147, `tasks.md` Phase 3-7  
**변경 내용**:

**tasks.md Phase 3에 엣지 케이스 테스트 작업 추가**:
```markdown
### 사용자 스토리 1을 위한 엣지 케이스 테스트

- [ ] T054.1 [P] [US1] tests/CollectionServer.UnitTests/EdgeCases/BarcodeEdgeCaseTests.cs 생성
  - 체크 디지트 오류 바코드 → 400 Bad Request
  - 공백/대시 포함 바코드 정규화 성공
  - 5자리 등 잘못된 길이 → 400 Bad Request
  - 숫자가 아닌 문자 포함 → 400 Bad Request
  
- [ ] T054.2 [P] [US1] tests/CollectionServer.UnitTests/EdgeCases/BookEdgeCaseTests.cs 생성
  - 여러 저자 배열 처리 (Authors = ["저자1", "저자2", "저자3"])
  - 표지 이미지 없는 도서 (CoverImageUrl = null)
  - 충돌하는 ISBN 데이터 (우선순위 소스 선택)
  - 설명 없는 도서 (Description = null)

- [ ] T054.3 [P] [US1] tests/CollectionServer.UnitTests/EdgeCases/MovieEdgeCaseTests.cs 생성
  - 동일 영화 Blu-ray/DVD 별도 항목 (Format 필드로 구분)
  - 여러 감독 배열 처리
  - 출연진 많은 영화 (상위 10명만 저장)
  - 미등급 콘텐츠 (Rating = "Unrated")

- [ ] T054.4 [P] [US1] tests/CollectionServer.UnitTests/EdgeCases/MusicAlbumEdgeCaseTests.cs 생성
  - 컴필레이션 앨범 (Artist = "Various Artists")
  - 다중 디스크 앨범 (Track.DiscNumber 처리)
  - 트랙 목록 없는 앨범 (Tracks = [])
  - 재발매 버전 (ReleasedDate vs OriginalReleaseDate)
```

**spec.md 엣지 케이스에 우선순위 추가**:
```markdown
## 엣지 케이스

### 높은 우선순위 (Phase 3-4에서 구현) ⭐
- ✅ 체크 디지트 오류 → 400 Bad Request
- ✅ 여러 저자/감독 → 배열 반환
- ✅ 표지 이미지 없음 → null 반환
- ✅ 바코드 서식 (공백/대시) → 정규화

### 중간 우선순위 (Phase 5-7에서 구현)
- 외부 API 충돌 데이터 → 우선순위 소스 사용
- UPC가 여러 미디어 타입 가능 → 순차 시도
- 다중 디스크 앨범 → 디스크 번호 포함
- 등급 정보 없음 → "Unrated" 반환

### 낮은 우선순위 (Phase 9 또는 미래)
- 박스 세트 트랙 목록 → 페이지네이션 고려
- 컴필레이션 "Various Artists" → 표준 처리
- Special Edition 버전 → 제목에 버전 정보 포함
```

#### U9: In-Memory DB 통합 테스트 한계
**위치**: `plan.md` Lines 690-747  
**변경 내용**:

**plan.md에 한계 문서화**:
```markdown
### ASP.NET Core 통합 테스트 (WebApplicationFactory)

**In-Memory DB 전략**:

✅ **장점**:
- 빠른 테스트 실행 (초당 수백 개 테스트)
- 외부 의존성 없음 (Docker, PostgreSQL 불필요)
- CI/CD 파이프라인 단순화

⚠️ **한계**:
- PostgreSQL 특화 기능 미지원:
  - 배열 타입 (text[], int[])
  - JSONB 컬럼
  - Full-Text Search (tsvector)
  - 트리거 및 함수
  - CHECK 제약 조건
- 동시성 제어 테스트 제한
- 실제 네트워크 I/O 성능 측정 불가

**권장 사항**:
- **단위 테스트**: In-Memory DB 사용 ✅
- **통합 테스트 (기본)**: In-Memory DB 사용 ✅
- **통합 테스트 (PostgreSQL 기능 필요)**: Testcontainers 사용 🐳

**Testcontainers 도입 (선택)**:
```csharp
// tests/CollectionServer.IntegrationTests/Fixtures/PostgresTestContainer.cs
public class PostgresTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();
    
    public string ConnectionString => _container.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

**tasks.md Phase 9에 Testcontainers 작업 추가** (선택):
```markdown
### 통합 테스트 강화 (선택)

- [ ] T188.1 [P] Testcontainers.PostgreSQL NuGet 패키지 추가
- [ ] T188.2 [P] tests/CollectionServer.IntegrationTests/Fixtures/PostgresTestContainerFixture.cs 생성
- [ ] T188.3 PostgreSQL 배열 타입 저장/조회 통합 테스트 (Authors, Genres, Tracks)
- [ ] T188.4 JSONB 트랙 목록 저장/조회 통합 테스트
- [ ] T188.5 동시성 제어 통합 테스트 (SemaphoreSlim 동작 확인)
```
```

#### G1: Retry-After 헤더 구현 작업 누락
**위치**: `spec.md` FR-026, `tasks.md` T156  
**변경 내용**:

**tasks.md T156 확장**:
```markdown
- [ ] T156 [US6] Rate Limit 초과 시 커스텀 응답 및 Retry-After 헤더 추가
  - 한국어 오류 메시지
  - Retry-After 헤더 (초 단위)
  - X-RateLimit-Limit 헤더 (100)
  - X-RateLimit-Remaining 헤더 (남은 요청 수)
  - X-RateLimit-Reset 헤더 (윈도우 리셋 시각, Unix timestamp)
```

**plan.md Rate Limiter 구성 업데이트**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
    
    // Rate Limit 초과 시 커스텀 응답
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        // Rate Limit 헤더 추가
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "100";
        context.HttpContext.Response.Headers["X-RateLimit-Window"] = "60"; // seconds
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        
        if (context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason))
        {
            context.HttpContext.Response.Headers["X-RateLimit-Reason"] = reason;
        }
        
        // 한국어 오류 응답
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMIT_EXCEEDED",
            message = "분당 요청 한도(100개)를 초과했습니다. 잠시 후 다시 시도하세요.",
            retryAfterSeconds = retryAfter?.TotalSeconds ?? 60,
            limit = 100,
            window = "1분"
        }, cancellationToken);
    };
});
```

#### G2: 엣지 케이스 테스트 작업 명시 부족
**위치**: `spec.md` Lines 115-147, `tasks.md`  
**변경 내용**: U8과 통합됨

#### I2: "Blu-ray/DVD" → "영화" 용어 통일 (전체 문서)
**위치**: `spec.md`, `plan.md`, `tasks.md`, `data-model.md` 등 다수  
**변경 내용**:

**일괄 치환 필요 (수동 확인 필수)**:
```bash
# 다음 치환을 모든 문서에 적용 (문맥 확인 후):
"Blu-ray/DVD" → "영화" (일반 언급)
"Blu-ray/DVD 정보" → "영화 정보"
"Blu-ray/DVD UPC" → "영화 UPC"
"Blu-ray/DVD 바코드" → "영화 바코드"

# 단, 다음은 유지:
"Blu-ray 또는 DVD 형식" (Format 필드 설명 시)
"형식(Blu-ray/DVD)" (필드 값 예시)
```

**주요 파일별 치환 위치**:
- spec.md: Lines 7, 13, 22, 31, 40, 92, 124-125, 178, 190-191, 213, 216
- plan.md: Lines 10, 40
- data-model.md: Line 160
- tasks.md: 여러 위치 (Movie.cs 언급)
- contracts/openapi.yaml: Line 5

**FR-017 명확화** (이미 수정됨):
```markdown
- **FR-017**: 시스템은 영화에 대해 다음 정보를 반환해야 합니다 (Blu-ray 또는 DVD 형식):
  - 제목, 감독(배열), 출연진(배열), 런타임(분), 발매일, 스튜디오, 등급(예: PG-13), 
    시놉시스, 커버 아트 URL, 장르(배열), **형식(Format: "Blu-ray" 또는 "DVD")**
```

#### A4: 우선순위 폴백 "더 완전한 데이터" 비교 모호
**위치**: `spec.md` Lines 94-95, `plan.md`  
**변경 내용**:

**Option A (권장): 간단한 전략 채택**
```markdown
# spec.md US2 시나리오 4 수정:
4. **주어진** 미디어가 여러 외부 데이터베이스에 존재할 때, **데이터가** 우선순위에 따라 검색되면, **우선순위가 높은 소스의 데이터를** 사용합니다

# FR-014 수정:
- **FR-014**: 시스템은 한 외부 API가 실패할 경우 다음 우선순위 소스로 폴백해야 합니다
  - **전략**: 첫 번째로 성공한 응답 (필수 필드 포함)을 사용
  - **데이터 병합**: 수행하지 않음 (단일 소스에서만 데이터 획득)
  - **예외**: 모든 소스 실패 시 NotFoundException 발생
```

**Option B (고급, Phase 9 이후): 데이터 완전성 점수** (선택):
```markdown
# data-model.md에 추가:

## 데이터 완전성 점수 계산 (Phase 9 이후 고려)

### 점수 산정 기준

| 필드 | Book | Movie | MusicAlbum | 설명 |
|------|------|-------|------------|------|
| 제목 (필수) | 30 | 30 | 30 | 없으면 무효 |
| 주요 메타 (저자/감독/아티스트) | 25 | 25 | 25 | 배열 크기에 비례 |
| 발매일 | 15 | 15 | 15 | - |
| 설명/시놉시스 | 10 | 10 | - | 길이에 비례 |
| 커버 이미지 | 10 | 10 | 10 | URL 유효성 |
| 카테고리/장르 | 5 | 5 | 10 | 배열 크기에 비례 |
| 추가 메타 | 5 | 5 | 10 | 페이지수, 런타임 등 |

### 멀티소스 전략 (선택 기능)
- 동일 바코드로 여러 소스 병렬 조회
- 각 응답의 완전성 점수 계산
- 최고 점수 응답 선택
- 동점 시 우선순위 높은 소스 선택
```

**권장**: Option A 선택하여 spec.md 간소화

#### D1: 성공 기준 중복 기록
**위치**: `spec.md` Lines 222-229, `tasks.md` Lines 618-644  
**변경 내용**:

**tasks.md에서 성공 기준 정의 제거, 측정 방법만 유지**:
```markdown
## 성공 기준 (Success Criteria)

**정의**: `/specs/001-isbn-book-api/spec.md` Lines 222-229의 SC-001 ~ SC-008 참조

### 측정 방법 및 검증 전략

#### SC-001: 95%+ 완전한 정보 반환
- **측정 도구**: `tests/CollectionServer.IntegrationTests/Data/StandardBarcodes.json` (100개 검증 세트)
- **실행 명령**: `dotnet test --filter FullyQualifiedName~StandardBarcodeTests`
- **통과 기준**: 95개 이상 200 OK 응답 + 필수 필드 존재 (Title, 메타데이터, ReleaseDate)
  
#### SC-002: 응답 시간 목표
- **측정 도구**: `tests/CollectionServer.IntegrationTests/PerformanceTests/ResponseTimeTests.cs`
- **실행 명령**: `dotnet test --filter FullyQualifiedName~ResponseTimeTests`
- **통과 기준**: 
  - 데이터베이스 조회: P99 < 500ms
  - 외부 API 조회: P99 < 2초
  
#### SC-003: 바코드 검증 정확도
- **측정 도구**: `tests/CollectionServer.UnitTests/Services/BarcodeValidatorTests.cs`
- **실행 명령**: `dotnet test --filter FullyQualifiedName~BarcodeValidatorTests`
- **통과 기준**: 1000개 테스트 케이스 중 999개 이상 정확 (99.9%)
  
#### SC-004: Database-First 캐싱 효과
- **측정 도구**: `tests/CollectionServer.IntegrationTests/PerformanceTests/CachingEffectTests.cs`
- **실행 명령**: 워크로드 시뮬레이션 (동일 바코드 20% 재조회)
- **통과 기준**: 외부 API 호출 80% 감소 (캐싱 히트 비율)
  
#### SC-005: 우선순위 폴백 성공률
- **측정 도구**: `tests/CollectionServer.IntegrationTests/ApiTests/PriorityFallbackTests.cs`
- **실행 명령**: 1순위 API Mock 실패 시나리오
- **통과 기준**: 90% 이상 2-3순위 폴백으로 성공
  
#### SC-006: Rate Limiting 정책
- **측정 도구**: `tests/CollectionServer.IntegrationTests/ApiTests/RateLimitingTests.cs`
- **실행 명령**: 1분 내 110개 요청 전송
- **통과 기준**: 100개 성공, 10개 429 응답, Retry-After 헤더 존재
  
#### SC-007: 오류 메시지 명확성
- **측정 도구**: `tests/CollectionServer.IntegrationTests/ApiTests/ErrorHandlingTests.cs`
- **실행 명령**: 모든 오류 시나리오 (400, 404, 429, 503) 테스트
- **통과 기준**: 모든 오류 응답에 한국어 메시지 + 해결 방법 포함
  
#### SC-008: 외부 API 장애 처리
- **측정 도구**: `tests/CollectionServer.IntegrationTests/ApiTests/ExternalApiFailureTests.cs`
- **실행 명령**: Mock으로 외부 API 타임아웃/500 오류 시뮬레이션
- **통과 기준**: 적절한 HTTP 상태 (503) + Retry 가능 메시지

### 종합 검증 스크립트

```bash
#!/bin/bash
# tests/run-success-criteria.sh

echo "=== Success Criteria 검증 시작 ==="

echo "SC-001: 완전한 정보 반환 (95%+)"
dotnet test --filter "FullyQualifiedName~StandardBarcodeTests" --logger "console;verbosity=minimal"

echo "SC-002: 응답 시간"
dotnet test --filter "FullyQualifiedName~ResponseTimeTests" --logger "console;verbosity=minimal"

echo "SC-003: 바코드 검증 정확도"
dotnet test --filter "FullyQualifiedName~BarcodeValidatorTests" --logger "console;verbosity=minimal"

echo "SC-004~SC-008: 나머지 기준"
dotnet test --filter "FullyQualifiedName~SuccessCriteriaTests" --logger "console;verbosity=minimal"

echo "=== 검증 완료 ==="
```
```

---

### LOW Priority (2개)

#### A5: 타임라인 예측 가정 모호
**위치**: `tasks.md` Lines 650-665  
**변경 내용**: (이미 수정 제안 완료)

#### 용어집, FAQ, 아키텍처 다이어그램 추가
**위치**: 다양한 문서  
**변경 내용**: (이미 용어집 추가 완료, 나머지는 Phase 9에서 처리)

---

## 📊 진행 상황 요약

| 우선순위 | 총 개수 | 완료 | 진행 중 | 남음 | 완료율 |
|---------|---------|------|---------|------|--------|
| CRITICAL | 2 | 2 | 0 | 0 | 100% ✅ |
| HIGH | 9 | 5 | 0 | 4 | 56% 🔄 |
| MEDIUM | 14 | 0 | 0 | 14 | 0% ⏳ |
| LOW | 2 | 1 | 0 | 1 | 50% ⏳ |
| **TOTAL** | **27** | **8** | **0** | **19** | **30%** |

---

## 🎯 다음 단계

### 즉시 실행 (1-2시간)
1. ✅ C1, C2 해결 완료
2. ✅ I1, I2 (부분), A3 해결 완료
3. ✅ 용어집 추가 완료
4. 🔄 I2 완료를 위한 "Blu-ray/DVD" → "영화" 일괄 치환 (수동 검토 필요)

### 단기 작업 (4-6시간)
5. I3-I5: API 명명 일관성 및 베이스 경로 명시
6. U1-U3: 성공 기준 및 Rate Limit 명확화
7. A1-A2: 모호한 용어 명확화

### 중기 작업 (4-6시간)
8. I6-I8, U4-U9: data-model.md 작성 및 기술 세부사항 명시
9. G1-G2: 누락 작업 추가

### 검토 및 마무리 (1-2시간)
10. D1: 중복 제거
11. 전체 문서 일관성 검토
12. /speckit.implement 준비

---

## 📝 수동 작업 필요 항목

다음 작업은 자동화 불가하므로 수동 실행 필요:

1. **"Blu-ray/DVD" → "영화" 일괄 치환** (I2)
   - spec.md, plan.md, tasks.md, data-model.md 등 전체 검색
   - 문맥 확인 후 선택적 치환 (Format 필드 설명은 유지)

2. **data-model.md 전체 작성** (I6)
   - ERD 다이어그램 추가
   - 모든 엔티티 필드 정의
   - Track 값 객체 상세화

3. **contracts/openapi.yaml servers 섹션 추가** (I5)

4. **quickstart.md 마이그레이션 섹션 작성** (U6)

5. **tasks.md 엣지 케이스 작업 추가** (U8, G2)

6. **Containerfile Healthcheck 명령 추가** (U7)

---

## ✅ 완료 조건

다음 조건이 모두 충족되면 Plan C 완료:

- [ ] CRITICAL 2개 해결 (✅ 완료)
- [ ] HIGH 9개 해결 (5/9 완료)
- [ ] MEDIUM 14개 해결 (0/14)
- [ ] LOW 2개 해결 (1/2)
- [ ] 헌장 위반 0개
- [ ] 모든 문서 일관성 검증
- [ ] /speckit.analyze 재실행하여 0 issues 확인

---

**예상 총 소요 시간**: 10-12시간  
**현재 진행률**: 30% (8/27 완료)  
**남은 시간**: 약 7-8시간
