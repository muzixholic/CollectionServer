---
description: "미디어 정보 API 서버 구현을 위한 작업 목록"
---

# 작업 목록 (Tasks): 미디어 정보 API 서버

**입력 (Input)**: `/specs/001-isbn-book-api/`의 설계 문서  
**전제조건 (Prerequisites)**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md  
**기술 스택 (Tech Stack)**: ASP.NET Core 10.0, C# 13, Entity Framework Core 10.0, PostgreSQL 16+

**테스트 (Tests)**: 이 프로젝트는 높은 신뢰성이 필요하므로 모든 계층에 대한 테스트 작업이 포함됩니다.

**조직화 (Organization)**: 작업은 사용자 스토리별로 그룹화되어 각 스토리의 독립적인 구현 및 테스트를 가능하게 합니다.

## 형식: `[ID] [P?] [Story] 설명`

- **[P]**: 병렬 실행 가능 (다른 파일, 종속성 없음)
- **[Story]**: 이 작업이 속한 사용자 스토리 (예: US1, US2, US3)
- 설명에 정확한 파일 경로 포함

## 경로 규칙 (Path Conventions)

프로젝트 구조 (plan.md 기준):
```
src/
├── CollectionServer.Api/           # ASP.NET Core Web API
├── CollectionServer.Core/          # 도메인 레이어
└── CollectionServer.Infrastructure/ # 인프라 레이어

tests/
├── CollectionServer.UnitTests/
├── CollectionServer.IntegrationTests/
└── CollectionServer.ContractTests/
```

---

## Phase 1: 설정 (Setup - 공유 인프라)

**목적 (Purpose)**: 프로젝트 초기화 및 기본 구조 생성

- [X] T001 global.json 생성하여 .NET SDK 10.0.100 버전 고정
- [X] T002 CollectionServer.sln 솔루션 파일 생성
- [X] T003 [P] src/CollectionServer.Api 프로젝트 생성 (ASP.NET Core 10 Web API)
- [X] T004 [P] src/CollectionServer.Core 프로젝트 생성 (클래스 라이브러리)
- [X] T005 [P] src/CollectionServer.Infrastructure 프로젝트 생성 (클래스 라이브러리)
- [X] T006 [P] tests/CollectionServer.UnitTests 프로젝트 생성 (xUnit)
- [X] T007 [P] tests/CollectionServer.IntegrationTests 프로젝트 생성 (xUnit)
- [X] T008 [P] tests/CollectionServer.ContractTests 프로젝트 생성 (xUnit)
- [X] T009 프로젝트 간 참조 추가 (Api → Core, Infrastructure; Infrastructure → Core)
- [X] T010 [P] Api 프로젝트에 필수 NuGet 패키지 추가 (Swashbuckle.AspNetCore, Serilog.AspNetCore)
- [X] T011 [P] Infrastructure 프로젝트에 EF Core 패키지 추가 (Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL)
- [X] T012 [P] 테스트 프로젝트에 테스트 프레임워크 패키지 추가 (xUnit, Moq, FluentAssertions)
- [X] T013 [P] .gitignore 파일 생성 (.NET 표준 템플릿)
- [X] T014 [P] README.md 파일 생성 (프로젝트 개요, 실행 방법)
- [X] T015 [P] Containerfile 생성 (Podman 빌드용 멀티 스테이지)
- [X] T016 [P] podman-compose.yml 생성 (PostgreSQL + API 서비스)

---

## Phase 2: 기반 (Foundational - 차단 전제조건)

**목적 (Purpose)**: 모든 사용자 스토리가 구현되기 전에 완료되어야 하는 핵심 인프라

**⚠️ 중요**: 이 단계가 완료될 때까지 사용자 스토리 작업을 시작할 수 없습니다

### 도메인 기반 작업

- [X] T017 [P] src/CollectionServer.Core/Enums/MediaType.cs 생성 (Book, Movie, MusicAlbum)
- [X] T018 [P] src/CollectionServer.Core/Enums/BarcodeType.cs 생성 (ISBN10, ISBN13, UPC, EAN13)
- [X] T019 [P] src/CollectionServer.Core/Entities/MediaItem.cs 추상 기본 클래스 생성
- [X] T020 [P] src/CollectionServer.Core/Entities/Book.cs 엔티티 생성
- [X] T021 [P] src/CollectionServer.Core/Entities/Movie.cs 엔티티 생성
- [X] T022 [P] src/CollectionServer.Core/Entities/MusicAlbum.cs 엔티티 생성
- [X] T023 [P] src/CollectionServer.Core/Entities/Track.cs 값 객체 생성 (음악 트랙용)
- [X] T024 [P] src/CollectionServer.Core/Exceptions/InvalidBarcodeException.cs 생성
- [X] T025 [P] src/CollectionServer.Core/Exceptions/NotFoundException.cs 생성
- [X] T026 [P] src/CollectionServer.Core/Exceptions/RateLimitExceededException.cs 생성
- [X] T027 [P] src/CollectionServer.Core/Interfaces/IMediaRepository.cs 인터페이스 정의
- [X] T028 [P] src/CollectionServer.Core/Interfaces/IMediaService.cs 인터페이스 정의
- [X] T029 [P] src/CollectionServer.Core/Interfaces/IMediaProvider.cs 인터페이스 정의 (외부 API용)

### 데이터베이스 기반 작업

- [X] T030 src/CollectionServer.Infrastructure/Data/ApplicationDbContext.cs 생성
- [X] T031 [P] src/CollectionServer.Infrastructure/Data/Configurations/MediaItemConfiguration.cs 생성 (Fluent API)
- [X] T032 [P] src/CollectionServer.Infrastructure/Data/Configurations/BookConfiguration.cs 생성 (Fluent API)
- [X] T033 [P] src/CollectionServer.Infrastructure/Data/Configurations/MovieConfiguration.cs 생성 (Fluent API)
- [X] T034 [P] src/CollectionServer.Infrastructure/Data/Configurations/MusicAlbumConfiguration.cs 생성 (Fluent API)
- [X] T035 EF Core 초기 마이그레이션 생성 (InitialCreate)
- [X] T036 PostgreSQL 데이터베이스 스키마 적용 (dotnet ef database update)

### 공통 서비스 기반 작업

- [X] T037 src/CollectionServer.Core/Services/BarcodeValidator.cs 구현 (체크섬 검증 포함)
- [X] T038 src/CollectionServer.Infrastructure/Repositories/MediaRepository.cs 구현 (IMediaRepository)
- [X] T039 src/CollectionServer.Infrastructure/Options/ExternalApiSettings.cs 생성 (Options 패턴)

### ASP.NET Core 기반 작업

- [X] T040 src/CollectionServer.Api/Program.cs 기본 설정 (WebApplicationBuilder, Minimal API 구조)
- [X] T041 src/CollectionServer.Api/appsettings.json 생성 (기본 설정, 연결 문자열 템플릿)
- [X] T042 src/CollectionServer.Api/appsettings.Development.json 생성 (개발 환경 설정)
- [X] T043 src/CollectionServer.Api/appsettings.Production.json 생성 (프로덕션 환경 설정)
- [X] T044 src/CollectionServer.Api/Middleware/ErrorHandlingMiddleware.cs 구현 (전역 예외 처리)
- [X] T045 src/CollectionServer.Api/Extensions/ServiceCollectionExtensions.cs 생성 (DI 확장 메서드)
- [X] T046 Program.cs에 Serilog 로깅 구성 추가 (JSON 포맷, 파일 출력)
- [X] T047 Program.cs에 Rate Limiting 미들웨어 추가 (100 req/min)
- [X] T048 Program.cs에 Swagger/OpenAPI 구성 추가 (한국어 설명 포함)
- [X] T049 Program.cs에 의존성 주입 구성 추가 (DbContext, Repositories, Services)

**체크포인트 (Checkpoint)**: 기반 준비 완료 - 이제 사용자 스토리 구현을 병렬로 시작할 수 있습니다

---

## Phase 3: 사용자 스토리 1 - 개발자의 미디어 조회 통합 (우선순위: P1) 🎯 MVP

**목표 (Goal)**: 바코드/ISBN으로 미디어 정보를 조회하는 핵심 API 엔드포인트 구현. Database-First 아키텍처로 PostgreSQL 우선 조회 후 외부 API 폴백.

**독립 테스트 (Independent Test)**: 유효한 ISBN-13으로 GET /items/{barcode} 호출 시 200 OK와 완전한 도서 정보 반환. 동일한 바코드 재요청 시 데이터베이스에서 빠르게 반환 (<500ms).

### 사용자 스토리 1을 위한 계약 테스트

- [ ] T050 [P] [US1] tests/CollectionServer.ContractTests/OpenApi/SwaggerSchemaTests.cs 생성 (OpenAPI 스키마 검증)
- [ ] T051 [P] [US1] tests/CollectionServer.ContractTests/Endpoints/MediaEndpointContractTests.cs 생성 (엔드포인트 응답 형식 검증)

### 사용자 스토리 1을 위한 단위 테스트

- [ ] T052 [P] [US1] tests/CollectionServer.UnitTests/Services/BarcodeValidatorTests.cs 생성 (ISBN-10/13, UPC, EAN 검증)
- [ ] T053 [P] [US1] tests/CollectionServer.UnitTests/Services/MediaServiceTests.cs 생성 (Database-First 로직 테스트)
- [ ] T054 [P] [US1] tests/CollectionServer.UnitTests/Repositories/MediaRepositoryTests.cs 생성 (CRUD 작업 테스트)

### 사용자 스토리 1을 위한 통합 테스트

- [ ] T055 [US1] tests/CollectionServer.IntegrationTests/Fixtures/TestWebApplicationFactory.cs 생성 (In-Memory DB 설정)
- [ ] T056 [US1] tests/CollectionServer.IntegrationTests/ApiTests/MediaEndpointTests.cs 생성 (E2E 테스트)
- [ ] T057 [US1] tests/CollectionServer.IntegrationTests/RepositoryTests/MediaRepositoryIntegrationTests.cs 생성 (실제 DB 작업 테스트)

### 사용자 스토리 1을 위한 핵심 구현

- [ ] T058 [US1] src/CollectionServer.Core/Services/MediaService.cs 구현 (IMediaService, Database-First 로직)
- [ ] T059 [US1] Program.cs에 MediaService DI 등록 추가
- [ ] T060 [US1] Program.cs에 GET /items/{barcode} Minimal API 엔드포인트 추가
- [ ] T061 [US1] Program.cs에 GET /health 헬스 체크 엔드포인트 추가
- [ ] T062 [US1] GET /items/{barcode}에 Rate Limiting 적용 (.RequireRateLimiting("api"))
- [ ] T063 [US1] GET /items/{barcode}에 OpenAPI 메타데이터 추가 (.WithOpenApi(), 한국어 설명)
- [ ] T064 [US1] MediaService에서 바코드 검증 로직 통합 (BarcodeValidator 사용)
- [ ] T065 [US1] MediaService에서 데이터베이스 조회 로직 구현 (MediaRepository 사용)
- [ ] T066 [US1] 바코드 형식별 미디어 타입 자동 감지 로직 구현

### 사용자 스토리 1을 위한 검증

- [ ] T067 [US1] 로컬 환경에서 dotnet run 실행 및 Swagger UI 접근 확인
- [ ] T068 [US1] GET /health 엔드포인트로 데이터베이스 연결 확인
- [ ] T069 [US1] 잘못된 바코드 형식으로 400 Bad Request 응답 검증
- [ ] T070 [US1] 존재하지 않는 바코드로 404 Not Found 응답 검증 (외부 API 없이)

**체크포인트 (Checkpoint)**: 이 시점에서 사용자 스토리 1은 완전히 기능하고 독립적으로 테스트 가능해야 합니다 (외부 API 통합 제외)

---

## Phase 4: 사용자 스토리 2 - 최종 사용자의 미디어 발견 (우선순위: P1)

**목표 (Goal)**: 외부 API 통합을 추가하여 다양한 미디어 유형(도서, 영화, 음악)의 완전한 정보를 제공. 우선순위 기반 폴백 전략 구현.

**독립 테스트 (Independent Test)**: 데이터베이스에 없는 유효한 ISBN-13으로 조회 시 Google Books API에서 정보를 가져와 200 OK 반환. 응답에 제목, 저자, 표지 이미지 URL 등 모든 필드 포함 확인.

### 사용자 스토리 2를 위한 단위 테스트

- [ ] T071 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/GoogleBooksProviderTests.cs 생성 (Mock HTTP 응답)
- [ ] T072 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/KakaoBookProviderTests.cs 생성
- [ ] T073 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/AladinApiProviderTests.cs 생성
- [ ] T074 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/TMDbProviderTests.cs 생성
- [ ] T075 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/OMDbProviderTests.cs 생성
- [ ] T076 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/MusicBrainzProviderTests.cs 생성
- [ ] T077 [P] [US2] tests/CollectionServer.UnitTests/ExternalApis/DiscogsProviderTests.cs 생성

### 사용자 스토리 2를 위한 통합 테스트

- [ ] T078 [US2] tests/CollectionServer.IntegrationTests/ApiTests/ExternalApiIntegrationTests.cs 생성 (실제 API 호출 테스트)
- [ ] T079 [US2] tests/CollectionServer.IntegrationTests/ApiTests/PriorityFallbackTests.cs 생성 (폴백 전략 테스트)

### 사용자 스토리 2를 위한 외부 API Provider 구현

- [ ] T080 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Books/GoogleBooksProvider.cs 구현 (IMediaProvider)
- [ ] T081 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Books/KakaoBookProvider.cs 구현
- [ ] T082 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Books/AladinApiProvider.cs 구현
- [ ] T083 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Movies/TMDbProvider.cs 구현
- [ ] T084 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Movies/OMDbProvider.cs 구현
- [ ] T085 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Music/MusicBrainzProvider.cs 구현
- [ ] T086 [P] [US2] src/CollectionServer.Infrastructure/ExternalApis/Music/DiscogsProvider.cs 구현

### 사용자 스토리 2를 위한 서비스 통합

- [ ] T087 [US2] Program.cs에 HttpClientFactory 구성 추가 (각 Provider별 BaseAddress, Timeout 설정)
- [ ] T088 [US2] Program.cs에 모든 IMediaProvider 구현체 DI 등록 (우선순위 포함)
- [ ] T089 [US2] ExternalApiSettings에 API 키, Base URL, Priority 설정 추가 (appsettings.json)
- [ ] T090 [US2] MediaService에 외부 API 우선순위 폴백 로직 추가 (OrderBy Priority)
- [ ] T091 [US2] MediaService에 외부 API 결과를 데이터베이스에 저장하는 로직 추가
- [ ] T092 [US2] 외부 API 호출 실패 시 로깅 추가 (Serilog Warning)
- [ ] T093 [US2] 모든 외부 API 실패 시 404 Not Found 반환 로직 구현

### 사용자 스토리 2를 위한 검증

- [ ] T094 [US2] User Secrets에 실제 API 키 설정 (dotnet user-secrets set)
- [ ] T095 [US2] 도서 바코드로 Google Books API 호출 검증 (실제 HTTP 요청)
- [ ] T096 [US2] 영화 UPC로 TMDb API 호출 검증
- [ ] T097 [US2] 음악 앨범 바코드로 MusicBrainz API 호출 검증
- [ ] T098 [US2] 외부 API에서 가져온 데이터가 데이터베이스에 저장되는지 확인
- [ ] T099 [US2] 동일한 바코드 재요청 시 데이터베이스에서 조회되는지 확인 (외부 API 호출 없음)

**체크포인트 (Checkpoint)**: 이 시점에서 사용자 스토리 1과 2가 모두 독립적으로 작동해야 합니다. 모든 미디어 유형에 대한 완전한 정보 조회 가능.

---

## Phase 5: 사용자 스토리 3 - 오류 처리 및 우아한 폴백 (우선순위: P1)

**목표 (Goal)**: 강력한 오류 처리 및 명확한 오류 메시지 제공. 클라이언트가 문제를 이해하고 해결할 수 있도록 지원.

**독립 테스트 (Independent Test)**: 잘못된 바코드 형식, 존재하지 않는 미디어, 외부 API 실패 등 모든 오류 시나리오에 대해 적절한 HTTP 상태 코드와 상세 오류 메시지 반환 확인.

### 사용자 스토리 3을 위한 단위 테스트

- [ ] T100 [P] [US3] tests/CollectionServer.UnitTests/Middleware/ErrorHandlingMiddlewareTests.cs 생성
- [ ] T101 [P] [US3] tests/CollectionServer.UnitTests/Services/ErrorResponseTests.cs 생성 (오류 응답 형식 검증)

### 사용자 스토리 3을 위한 통합 테스트

- [ ] T102 [US3] tests/CollectionServer.IntegrationTests/ApiTests/ErrorHandlingTests.cs 생성 (모든 오류 시나리오 E2E)
- [ ] T103 [US3] tests/CollectionServer.IntegrationTests/ApiTests/RateLimitingTests.cs 생성 (429 응답 검증)

### 사용자 스토리 3을 위한 구현

- [ ] T104 [P] [US3] src/CollectionServer.Api/Models/ErrorResponse.cs 생성 (오류 응답 DTO)
- [ ] T105 [US3] ErrorHandlingMiddleware에 InvalidBarcodeException 처리 추가 (400 Bad Request)
- [ ] T106 [US3] ErrorHandlingMiddleware에 NotFoundException 처리 추가 (404 Not Found)
- [ ] T107 [US3] ErrorHandlingMiddleware에 RateLimitExceededException 처리 추가 (429 Too Many Requests)
- [ ] T108 [US3] ErrorHandlingMiddleware에 일반 Exception 처리 추가 (500 Internal Server Error)
- [ ] T109 [US3] ErrorHandlingMiddleware에 한국어 오류 메시지 추가
- [ ] T110 [US3] Program.cs의 Minimal API 엔드포인트에 Result 타입 반환 추가 (TypedResults 사용)
- [ ] T111 [US3] OpenAPI 스키마에 모든 오류 응답 명세 추가 (.Produces<ErrorResponse>)
- [ ] T112 [US3] BarcodeValidator에서 상세 검증 오류 메시지 생성 (예상 형식 설명 포함)
- [ ] T113 [US3] MediaService에서 외부 API 실패 시 상세 로그 추가 (어떤 소스가 실패했는지)

### 사용자 스토리 3을 위한 검증

- [ ] T114 [US3] 잘못된 바코드 형식 (5자리)으로 400 응답 및 오류 메시지 확인
- [ ] T115 [US3] 체크섬 오류가 있는 바코드로 400 응답 확인
- [ ] T116 [US3] 존재하지 않는 유효한 바코드로 404 응답 확인 (모든 소스 조회 후)
- [ ] T117 [US3] Rate Limit 초과 시 429 응답 및 Retry-After 헤더 확인
- [ ] T118 [US3] 데이터베이스 연결 실패 시 503 Service Unavailable 응답 확인
- [ ] T119 [US3] 모든 오류 응답이 OpenAPI 스키마와 일치하는지 확인

**체크포인트 (Checkpoint)**: 이제 모든 오류 시나리오가 적절하게 처리되고 명확한 메시지를 반환합니다.

---

## Phase 6: 사용자 스토리 4 - Database-First 아키텍처를 통한 성능 최적화 (우선순위: P1)

**목표 (Goal)**: 데이터베이스 우선 조회를 통한 성능 최적화. 캐싱 효과로 응답 시간 단축 및 외부 API 의존성 감소.

**독립 테스트 (Independent Test)**: 동일한 바코드를 여러 번 요청했을 때 첫 요청은 외부 API 호출(>1초), 후속 요청은 데이터베이스에서 조회(<500ms) 확인.

### 사용자 스토리 4를 위한 단위 테스트

- [ ] T120 [P] [US4] tests/CollectionServer.UnitTests/Repositories/DatabasePerformanceTests.cs 생성 (쿼리 성능 테스트)
- [ ] T121 [P] [US4] tests/CollectionServer.UnitTests/Services/CachingLogicTests.cs 생성

### 사용자 스토리 4를 위한 통합 테스트

- [ ] T122 [US4] tests/CollectionServer.IntegrationTests/PerformanceTests/ResponseTimeTests.cs 생성 (응답 시간 측정)
- [ ] T123 [US4] tests/CollectionServer.IntegrationTests/PerformanceTests/ConcurrentRequestTests.cs 생성 (동시 요청 처리)

### 사용자 스토리 4를 위한 최적화 구현

- [ ] T124 [US4] MediaRepository에 Compiled Queries 추가 (EF.CompileAsyncQuery)
- [ ] T125 [US4] MediaRepository에 AsNoTracking 추가 (읽기 전용 쿼리)
- [ ] T126 [US4] ApplicationDbContext에 인덱스 추가 (Barcode UNIQUE INDEX)
- [ ] T127 [US4] MediaService에 동시 요청 처리 로직 추가 (SemaphoreSlim으로 중복 호출 방지)
- [ ] T128 [US4] Program.cs에 데이터베이스 연결 풀 설정 추가 (MaxPoolSize, Timeout)
- [ ] T129 [US4] Serilog에 응답 시간 로깅 추가 (데이터베이스 vs 외부 API 구분)
- [ ] T130 [US4] ApplicationDbContext에 UpdatedAt 자동 업데이트 트리거 설정 (PostgreSQL)

### 사용자 스토리 4를 위한 검증

- [ ] T131 [US4] 새로운 바코드 조회 시 응답 시간 측정 (외부 API 호출)
- [ ] T132 [US4] 동일한 바코드 재조회 시 응답 시간 측정 (데이터베이스 조회)
- [ ] T133 [US4] 응답 시간이 목표치 내인지 확인 (DB: <500ms, API: <2초)
- [ ] T134 [US4] 동시에 100개 요청 전송 시 모두 성공적으로 처리되는지 확인
- [ ] T135 [US4] Serilog 로그에서 데이터베이스 히트 vs API 호출 비율 확인

**체크포인트 (Checkpoint)**: Database-First 아키텍처가 완전히 작동하며 성능 목표를 달성합니다.

---

## Phase 7: 사용자 스토리 5 - 외부 데이터 소스 우선순위 및 폴백 (우선순위: P1)

**목표 (Goal)**: 각 미디어 유형에 대해 최적의 외부 API를 우선순위에 따라 조회하고, 실패 시 자동으로 다음 소스로 폴백.

**독립 테스트 (Independent Test)**: 첫 번째 우선순위 API를 Mock으로 실패시켰을 때 두 번째 우선순위 API로 자동 폴백하여 정상 응답 반환 확인.

### 사용자 스토리 5를 위한 단위 테스트

- [ ] T136 [P] [US5] tests/CollectionServer.UnitTests/Services/PriorityStrategyTests.cs 생성 (우선순위 로직 테스트)
- [ ] T137 [P] [US5] tests/CollectionServer.UnitTests/Services/FallbackLogicTests.cs 생성

### 사용자 스토리 5를 위한 통합 테스트

- [ ] T138 [US5] tests/CollectionServer.IntegrationTests/ApiTests/MultiSourceFallbackTests.cs 생성 (여러 소스 폴백 시나리오)

### 사용자 스토리 5를 위한 구현

- [ ] T139 [US5] IMediaProvider에 Priority 속성 추가 (int, 낮을수록 높은 우선순위)
- [ ] T140 [US5] 각 Provider 구현체에 Priority 값 설정 (GoogleBooks: 1, Kakao: 2, Aladin: 3...)
- [ ] T141 [US5] MediaService에 Provider 정렬 로직 추가 (_providers.OrderBy(p => p.Priority))
- [ ] T142 [US5] MediaService에 폴백 루프 구현 (foreach provider, try-catch)
- [ ] T143 [US5] 각 Provider에 HTTP Timeout 설정 (10초)
- [ ] T144 [US5] 각 Provider에 실패 시 null 반환 로직 추가 (예외 대신)
- [ ] T145 [US5] MediaService에 모든 소스 실패 시 로깅 추가 (어떤 소스들을 시도했는지)
- [ ] T146 [US5] ExternalApiSettings에 각 API별 우선순위 구성 추가 (appsettings.json)

### 사용자 스토리 5를 위한 검증

- [ ] T147 [US5] 도서 조회 시 Google Books → Kakao → Aladin 순서로 시도하는지 로그 확인
- [ ] T148 [US5] Google Books API를 Mock으로 실패시키고 Kakao에서 성공하는지 확인
- [ ] T149 [US5] 모든 도서 API 실패 시 404 Not Found 반환 확인
- [ ] T150 [US5] 영화 조회 시 TMDb → OMDb 순서 확인
- [ ] T151 [US5] 음악 조회 시 MusicBrainz → Discogs 순서 확인

**체크포인트 (Checkpoint)**: 우선순위 기반 폴백 전략이 모든 미디어 유형에 대해 작동합니다.

---

## Phase 8: 사용자 스토리 6 - 속도 제한을 통한 공정한 API 사용 (우선순위: P2)

**목표 (Goal)**: Rate Limiting을 통해 API 악용 방지 및 모든 사용자에게 공정한 접근 보장.

**독립 테스트 (Independent Test)**: 1분 내에 100개 이상의 요청 전송 시 101번째 요청부터 429 Too Many Requests 응답 확인. Retry-After 헤더 포함 확인.

### 사용자 스토리 6을 위한 단위 테스트

- [ ] T152 [P] [US6] tests/CollectionServer.UnitTests/Middleware/RateLimitingTests.cs 생성

### 사용자 스토리 6을 위한 통합 테스트

- [ ] T153 [US6] tests/CollectionServer.IntegrationTests/ApiTests/RateLimitEnforcementTests.cs 생성 (실제 Rate Limit 검증)

### 사용자 스토리 6을 위한 구현

- [ ] T154 [US6] Program.cs에 AddRateLimiter 구성 확인 (이미 Phase 2에서 추가됨)
- [ ] T155 [US6] Rate Limiting 정책 세부 조정 (PermitLimit: 100, Window: 1분, QueueLimit: 10)
- [ ] T156 [US6] Rate Limit 초과 시 커스텀 응답 메시지 추가 (한국어)
- [ ] T157 [US6] Rate Limit 설정을 appsettings.json으로 외부화
- [ ] T158 [US6] Serilog에 Rate Limit 이벤트 로깅 추가

### 사용자 스토리 6을 위한 검증

- [ ] T159 [US6] 1분 내 100개 요청 전송하여 모두 성공하는지 확인
- [ ] T160 [US6] 101번째 요청에서 429 응답 확인
- [ ] T161 [US6] 429 응답에 Retry-After 헤더가 포함되는지 확인
- [ ] T162 [US6] 1분 경과 후 다시 요청 가능한지 확인

**체크포인트 (Checkpoint)**: Rate Limiting이 정상 작동하며 악용을 방지합니다.

---

## Phase 9: 다듬기 및 교차 관심사 (Polish & Cross-Cutting Concerns)

**목적 (Purpose)**: 여러 사용자 스토리에 영향을 미치는 개선사항 및 프로덕션 준비

### 문서화

- [ ] T163 [P] README.md 업데이트 (실행 방법, API 엔드포인트, 예제)
- [ ] T164 [P] API 사용 가이드 작성 (docs/api-guide.md)
- [ ] T165 [P] 배포 가이드 작성 (docs/deployment.md, Podman 포함)
- [ ] T166 [P] quickstart.md 검증 실행 (처음부터 끝까지 테스트)

### 코드 품질

- [ ] T167 코드 리뷰 및 리팩토링 (중복 제거, 명명 규칙 통일)
- [ ] T168 [P] XML 문서 주석 추가 (공개 API, 복잡한 로직에 한국어 주석)
- [ ] T169 [P] EditorConfig 파일 생성 (.NET 코딩 스타일)
- [ ] T170 [P] 단위 테스트 커버리지 확인 (최소 80% 목표)

### 보안

- [ ] T171 User Secrets 사용 가이드 작성 (로컬 개발용)
- [ ] T172 [P] 환경 변수로 민감 정보 주입 방법 문서화 (프로덕션용)
- [ ] T173 SQL Injection 방지 검증 (EF Core 파라미터화 쿼리 사용)
- [ ] T174 [P] HTTPS 강제 적용 설정 (프로덕션 환경)

### 성능 및 모니터링

- [ ] T175 [P] Application Insights 또는 Prometheus 메트릭 추가 (선택)
- [ ] T176 데이터베이스 인덱스 효율성 검증 (EXPLAIN ANALYZE)
- [ ] T177 [P] Serilog 구조화된 로깅 검증 (JSON 형식, 필요한 필드 포함)
- [ ] T178 메모리 및 CPU 사용량 프로파일링

### 컨테이너화 및 배포

- [ ] T179 Containerfile 빌드 테스트 (podman build)
- [ ] T180 podman-compose로 전체 스택 실행 테스트 (PostgreSQL + API)
- [ ] T181 컨테이너 이미지 크기 최적화 (멀티 스테이지 빌드 검증)
- [ ] T182 [P] 컨테이너 헬스 체크 구성 (HEALTHCHECK 명령)

### 최종 검증

- [ ] T183 전체 통합 테스트 스위트 실행 (dotnet test)
- [ ] T184 Swagger UI에서 모든 엔드포인트 수동 테스트
- [ ] T185 OpenAPI 스키마 검증 (Spectral 또는 Swagger Editor)
- [ ] T186 프로덕션 환경 설정 검토 (appsettings.Production.json)
- [ ] T187 [P] 장애 시나리오 테스트 (데이터베이스 다운, 외부 API 다운)
- [ ] T188 quickstart.md 가이드대로 처음부터 설치 후 동작 확인

---

## 의존성 및 실행 순서 (Dependencies & Execution Order)

### 단계 의존성 (Phase Dependencies)

```
Phase 1 (설정)
    ↓
Phase 2 (기반) ← 모든 사용자 스토리를 차단
    ↓
Phase 3 (US1) ← MVP 🎯
    ↓
Phase 4 (US2) ← 외부 API 통합
    ↓
Phase 5 (US3) ← 오류 처리
    ↓
Phase 6 (US4) ← 성능 최적화
    ↓
Phase 7 (US5) ← 폴백 전략
    ↓
Phase 8 (US6) ← Rate Limiting
    ↓
Phase 9 (다듬기)
```

### 사용자 스토리 의존성

- **사용자 스토리 1 (P1)**: Phase 2 이후 시작 가능 - 다른 스토리에 대한 의존성 없음
- **사용자 스토리 2 (P1)**: US1에 의존 (MediaService 기반 필요)
- **사용자 스토리 3 (P1)**: US1, US2에 의존 (오류 시나리오 테스트 위해)
- **사용자 스토리 4 (P1)**: US1, US2에 의존 (성능 비교 위해)
- **사용자 스토리 5 (P1)**: US2에 의존 (외부 API Provider 필요)
- **사용자 스토리 6 (P2)**: 독립적 (기반 작업만 필요)

### 각 사용자 스토리 내 순서

1. 계약 테스트 (Contract Tests) - 먼저 작성, 실패 확인
2. 단위 테스트 (Unit Tests) - 먼저 작성, 실패 확인
3. 통합 테스트 (Integration Tests) - 먼저 작성, 실패 확인
4. 모델/엔티티 (Models/Entities) - 병렬 실행 가능 [P]
5. 서비스 구현 (Services) - 모델 이후
6. 엔드포인트 구현 (Endpoints) - 서비스 이후
7. 검증 (Validation) - 구현 완료 후

### 병렬 기회 (Parallel Opportunities)

#### Phase 1 (설정)
```bash
# 동시 실행 가능:
T003, T004, T005  # 3개 프로젝트 생성
T006, T007, T008  # 3개 테스트 프로젝트 생성
T010, T011, T012, T013, T014, T015, T016  # 패키지 및 설정 파일
```

#### Phase 2 (기반)
```bash
# 동시 실행 가능:
T017, T018  # Enum 생성
T019, T020, T021, T022, T023  # 엔티티 생성
T024, T025, T026  # 예외 클래스
T027, T028, T029  # 인터페이스
T031, T032, T033, T034  # EF Core Configuration
```

#### Phase 3 (US1)
```bash
# 동시 실행 가능:
T050, T051  # 계약 테스트
T052, T053, T054  # 단위 테스트
```

#### Phase 4 (US2)
```bash
# 동시 실행 가능:
T071, T072, T073, T074, T075, T076, T077  # Provider 단위 테스트
T080, T081, T082, T083, T084, T085, T086  # Provider 구현
```

---

## 병렬 실행 예시

### 사용자 스토리 1 병렬 작업

```bash
# 동시에 작업 가능:
작업 T050: "계약 테스트 - Swagger 스키마 검증"
작업 T051: "계약 테스트 - 엔드포인트 응답 형식"
작업 T052: "단위 테스트 - BarcodeValidator"
작업 T053: "단위 테스트 - MediaService"
작업 T054: "단위 테스트 - MediaRepository"
```

### 사용자 스토리 2 병렬 작업

```bash
# 동시에 작업 가능:
작업 T080: "GoogleBooksProvider 구현"
작업 T081: "KakaoBookProvider 구현"
작업 T082: "AladinApiProvider 구현"
작업 T083: "TMDbProvider 구현"
작업 T084: "OMDbProvider 구현"
작업 T085: "MusicBrainzProvider 구현"
작업 T086: "DiscogsProvider 구현"
```

---

## 구현 전략 (Implementation Strategy)

### MVP 우선 (최소 기능 제품)

**목표**: 가능한 빠르게 가치 제공

```
1. Phase 1 완료 (T001-T016): 프로젝트 설정
2. Phase 2 완료 (T017-T049): 기반 인프라
3. Phase 3 완료 (T050-T070): 사용자 스토리 1
   - 바코드로 미디어 조회 (외부 API 없이 데이터베이스만)
   - 기본 검증 및 오류 처리
4. 검증 및 데모
5. Phase 4 추가 (T071-T099): 외부 API 통합
6. 재검증 및 배포
```

**MVP 체크리스트**:
- ✅ GET /items/{barcode} 엔드포인트 작동
- ✅ ISBN-13 검증
- ✅ 데이터베이스에 저장된 미디어 조회 가능
- ✅ 기본 오류 처리 (400, 404)
- ✅ Swagger UI 접근 가능
- ✅ 헬스 체크 엔드포인트 작동

### 점진적 제공 (Incremental Delivery)

각 단계 완료 후 배포 가능:

1. **Phase 3 완료** → MVP 배포 (내부 데이터베이스 조회만)
2. **Phase 4 완료** → 외부 API 통합 버전 배포 (도서, 영화, 음악 모두 지원)
3. **Phase 5 완료** → 강화된 오류 처리 버전
4. **Phase 6 완료** → 성능 최적화 버전
5. **Phase 7 완료** → 폴백 전략 강화 버전
6. **Phase 8 완료** → Rate Limiting 추가 버전
7. **Phase 9 완료** → 프로덕션 준비 완료

### 병렬 팀 전략

**3명의 개발자가 있다면:**

1. **Phase 1-2**: 모두 함께 기반 작업
2. **Phase 2 완료 후 분담**:
   - 개발자 A: Phase 3 (US1) - 핵심 엔드포인트
   - 개발자 B: Phase 4 (US2) - 외부 API Provider 구현
   - 개발자 C: Phase 5 (US3) - 오류 처리 강화
3. **통합 및 테스트**: 함께 검증
4. **Phase 6-8**: 순차적 또는 병렬 작업
5. **Phase 9**: 함께 다듬기

---

## 참고사항 (Notes)

### 코딩 가이드라인

- **한국어 사용**: 복잡한 로직에 한국어 주석 추가 (헌장 요구사항)
- **Async/Await**: 모든 I/O 작업은 비동기 (Database, HTTP 요청)
- **의존성 주입**: 모든 서비스는 생성자 주입
- **Options 패턴**: 설정은 IOptions<T> 사용
- **로깅**: 중요한 이벤트는 Serilog로 로깅 (Info, Warning, Error 구분)
- **예외 처리**: 비즈니스 예외는 커스텀 예외 클래스 사용

### 테스트 가이드라인

- **AAA 패턴**: Arrange, Act, Assert 명확히 구분
- **FluentAssertions**: 가독성 높은 Assertion 사용
- **Moq**: 외부 의존성 Mocking
- **xUnit**: [Fact], [Theory] 활용
- **테스트 격리**: 각 테스트는 독립적으로 실행 가능해야 함

### Git 커밋 전략

- 각 작업 완료 후 커밋
- 커밋 메시지 형식: `[T###] 작업 설명 (예: [T001] global.json 생성)`
- Phase 완료 후 브랜치 머지
- 테스트 실패 시 커밋 금지

### 피해야 할 것

- ❌ 동일 파일에 여러 사람이 동시 작업 (충돌 발생)
- ❌ 테스트 없이 구현 (TDD 원칙 위반)
- ❌ 하드코딩된 API 키 (User Secrets 사용)
- ❌ 동기 I/O 작업 (성능 저하)
- ❌ 글로벌 상태 (Thread-Safe 문제)
- ❌ 비밀번호 평문 저장 (환경 변수 또는 Secrets 사용)

---

## 성공 기준 (Success Criteria)

### 기능 성공 기준

- ✅ GET /items/{barcode} 엔드포인트가 모든 바코드 형식 지원 (ISBN-10/13, UPC, EAN-13)
- ✅ Database-First 아키텍처 작동 (데이터베이스 우선 조회, 외부 API 폴백)
- ✅ 7개 외부 API 모두 통합 (Google Books, Kakao, Aladin, TMDb, OMDb, MusicBrainz, Discogs)
- ✅ 우선순위 기반 폴백 전략 작동 (한 소스 실패 시 다음 소스로 자동 전환)
- ✅ 모든 오류 시나리오에 대해 적절한 HTTP 상태 코드 반환 (400, 404, 429, 500, 503)
- ✅ Rate Limiting 작동 (100 req/min)
- ✅ OpenAPI/Swagger 문서 생성 및 접근 가능

### 성능 성공 기준

- ✅ 데이터베이스 조회 응답 시간 < 500ms
- ✅ 외부 API 조회 응답 시간 < 2초
- ✅ 동시 100개 요청 처리 가능
- ✅ 데이터베이스 캐싱으로 외부 API 호출 80% 이상 감소

### 테스트 성공 기준

- ✅ 단위 테스트 커버리지 80% 이상
- ✅ 모든 통합 테스트 통과
- ✅ 계약 테스트로 OpenAPI 스키마 검증
- ✅ 오류 시나리오 E2E 테스트 통과

### 배포 성공 기준

- ✅ Podman 컨테이너로 빌드 및 실행 가능
- ✅ podman-compose로 전체 스택 실행 가능
- ✅ quickstart.md 가이드대로 처음부터 설치 가능
- ✅ 프로덕션 환경 설정 완료 (HTTPS, 환경 변수)

---

## 타임라인 예측 (Estimated Timeline)

**가정**: 1명의 풀타임 개발자

- **Phase 1 (설정)**: 1일 (T001-T016)
- **Phase 2 (기반)**: 3-4일 (T017-T049)
- **Phase 3 (US1)**: 2-3일 (T050-T070)
- **Phase 4 (US2)**: 4-5일 (T071-T099) - 7개 외부 API 통합
- **Phase 5 (US3)**: 1-2일 (T100-T119)
- **Phase 6 (US4)**: 1-2일 (T120-T135)
- **Phase 7 (US5)**: 1일 (T136-T151)
- **Phase 8 (US6)**: 1일 (T152-T162)
- **Phase 9 (다듬기)**: 2-3일 (T163-T188)

**총 예상 기간**: 16-22일 (약 3-4주)

**병렬 팀 (3명)**: 약 10-14일 (2주)

---

**작업 목록 생성 완료** ✅

**다음 단계**: `/speckit.implement` 명령으로 작업 시작

**총 작업 수**: 188개
- 설정: 16개
- 기반: 33개
- US1 (MVP): 21개
- US2 (외부 API): 29개
- US3 (오류 처리): 20개
- US4 (성능): 16개
- US5 (폴백): 16개
- US6 (Rate Limit): 11개
- 다듬기: 26개

**병렬 실행 가능 작업**: 약 60개 ([P] 표시)
**MVP 필수 작업**: T001-T070 (70개, 약 1주)
