# Tasks: 미디어 정보 API 서버

**Feature**: 001-isbn-book-api  
**Date**: 2025-11-16  
**Input**: Design documents from `/specs/001-isbn-book-api/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: ⚠️ 테스트 작업은 명세서에 명시적으로 요청되어 포함됩니다.

**Organization**: 사용자 스토리별로 작업을 그룹화하여 각 스토리를 독립적으로 구현하고 테스트할 수 있도록 합니다.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: 병렬 실행 가능 (다른 파일, 의존성 없음)
- **[Story]**: 사용자 스토리 레이블 (예: US1, US2, US3) - 사용자 스토리 페이즈에만 적용
- 설명에 정확한 파일 경로 포함

---

## Phase 1: 프로젝트 설정 (Sprint 0)

**목적**: 프로젝트 초기화 및 기본 구조 생성

**예상 시간**: 4-6시간

### 프로젝트 구조 및 의존성

- [ ] T001 리포지토리 루트에 global.json 생성 (.NET 10.0.100 SDK 지정)
- [ ] T002 솔루션 파일 생성 (CollectionServer.sln)
- [ ] T003 [P] API 프로젝트 생성 (src/CollectionServer.Api/ - ASP.NET Core 10.0 Web API, net10.0)
- [ ] T004 [P] Core 프로젝트 생성 (src/CollectionServer.Core/ - 클래스 라이브러리, net10.0)
- [ ] T005 [P] Infrastructure 프로젝트 생성 (src/CollectionServer.Infrastructure/ - 클래스 라이브러리, net10.0)
- [ ] T006 [P] 단위 테스트 프로젝트 생성 (tests/CollectionServer.UnitTests/ - xUnit, net10.0)
- [ ] T007 [P] 통합 테스트 프로젝트 생성 (tests/CollectionServer.IntegrationTests/ - xUnit, net10.0)
- [ ] T008 [P] 계약 테스트 프로젝트 생성 (tests/CollectionServer.ContractTests/ - xUnit, net10.0)
- [ ] T009 프로젝트 참조 구성 (Api → Core/Infrastructure, Infrastructure → Core, Tests → 해당 프로젝트)
- [ ] T010 각 .csproj에 LangVersion 13.0 및 Nullable enable 설정

### NuGet 패키지 설치

- [ ] T011 [P] API 프로젝트에 패키지 추가 (Microsoft.AspNetCore.OpenApi 10.0.0, Swashbuckle.AspNetCore 7.0.0, Serilog.AspNetCore 10.0.0)
- [ ] T012 [P] Infrastructure 프로젝트에 EF Core 패키지 추가 (Microsoft.EntityFrameworkCore 10.0.0, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0, Microsoft.EntityFrameworkCore.Design 10.0.0)
- [ ] T013 [P] 테스트 프로젝트에 패키지 추가 (xUnit 2.9.0, Moq 4.20.0, FluentAssertions 6.12.0, Microsoft.NET.Test.Sdk 17.11.0)
- [ ] T014 전체 솔루션 빌드 및 패키지 복원 검증

### 개발 환경 설정

- [ ] T015 [P] Docker Compose 파일 생성 (docker/docker-compose.yml - PostgreSQL 16 컨테이너 설정)
- [ ] T016 [P] Dockerfile 생성 (docker/Dockerfile - .NET 10.0 multi-stage build)
- [ ] T017 [P] .gitignore 파일 구성 (.NET 프로젝트용)
- [ ] T018 [P] appsettings.json 및 appsettings.Development.json 구성 (src/CollectionServer.Api/)
- [ ] T019 [P] User Secrets 초기화 및 외부 API 키 플레이스홀더 설정

**Checkpoint**: 프로젝트 구조 완성, 빌드 성공, Docker 컨테이너 실행 가능

---

## Phase 2: 기반 인프라 (Sprint 0 - 모든 사용자 스토리 차단)

**목적**: 모든 사용자 스토리가 의존하는 핵심 인프라

**예상 시간**: 12-16시간

**⚠️ 중요**: 이 페이즈가 완료되기 전에는 어떤 사용자 스토리 작업도 시작할 수 없습니다.

### 도메인 모델 및 엔티티

- [ ] T020 MediaType enum 생성 (src/CollectionServer.Core/Models/MediaType.cs - Book, Movie, MusicAlbum)
- [ ] T021 MediaItem 추상 기본 클래스 생성 (src/CollectionServer.Core/Models/MediaItem.cs - Id, Barcode, MediaType, Title, Description, ImageUrl, Source, CreatedAt, UpdatedAt)
- [ ] T022 [P] Book 엔티티 생성 (src/CollectionServer.Core/Models/Book.cs - MediaItem 상속, Isbn13, Authors, Publisher, PublishDate, PageCount, Genre)
- [ ] T023 [P] Movie 엔티티 생성 (src/CollectionServer.Core/Models/Movie.cs - MediaItem 상속, Director, Cast, RuntimeMinutes, ReleaseDate, Rating, Genre)
- [ ] T024 [P] MusicAlbum 엔티티 생성 (src/CollectionServer.Core/Models/MusicAlbum.cs - MediaItem 상속, Artist, Tracks, ReleaseDate, Label, Genre)
- [ ] T025 [P] Track 값 객체 생성 (src/CollectionServer.Core/Models/Track.cs - TrackNumber, Title, DurationSeconds)

### 데이터베이스 인프라

- [ ] T026 ApplicationDbContext 생성 (src/CollectionServer.Infrastructure/Data/ApplicationDbContext.cs - DbSet<MediaItem>, DbSet<Book>, DbSet<Movie>, DbSet<MusicAlbum>)
- [ ] T027 [P] BookConfiguration 생성 (src/CollectionServer.Infrastructure/Data/Configurations/BookConfiguration.cs - TPT 전략, PostgreSQL 배열 매핑)
- [ ] T028 [P] MovieConfiguration 생성 (src/CollectionServer.Infrastructure/Data/Configurations/MovieConfiguration.cs - TPT 전략, PostgreSQL 배열 매핑)
- [ ] T029 [P] MusicAlbumConfiguration 생성 (src/CollectionServer.Infrastructure/Data/Configurations/MusicAlbumConfiguration.cs - TPT 전략, JSONB 매핑)
- [ ] T030 [P] MediaItemConfiguration 생성 (src/CollectionServer.Infrastructure/Data/Configurations/MediaItemConfiguration.cs - 인덱스 설정: Barcode UNIQUE, MediaType)
- [ ] T031 EF Core 초기 마이그레이션 생성 (src/CollectionServer.Infrastructure/Migrations/ - InitialCreate)
- [ ] T032 PostgreSQL 데이터베이스 생성 및 마이그레이션 적용 (Docker Compose 또는 로컬)

### Repository 패턴

- [ ] T033 IMediaRepository 인터페이스 정의 (src/CollectionServer.Core/Interfaces/IMediaRepository.cs - GetByBarcodeAsync, AddAsync, UpdateAsync, ExistsAsync, GetRecentAsync)
- [ ] T034 MediaRepository 구현 (src/CollectionServer.Infrastructure/Repositories/MediaRepository.cs - EF Core 기반 구현, AsNoTracking 최적화)

### 바코드 검증 및 감지

- [ ] T035 BarcodeType enum 생성 (src/CollectionServer.Core/Models/BarcodeType.cs - ISBN10, ISBN13Book, EAN13Media, UPCMedia)
- [ ] T036 BarcodeValidator 클래스 생성 (src/CollectionServer.Core/Validators/BarcodeValidator.cs - ISBN-10/13, UPC, EAN-13 체크섬 검증)
- [ ] T037 BarcodeDetectionService 생성 (src/CollectionServer.Core/Services/BarcodeDetectionService.cs - 바코드 형식 자동 감지 및 정규화)

### 외부 API 통합 기반

- [ ] T038 IMediaProvider 인터페이스 정의 (src/CollectionServer.Core/Interfaces/IMediaProvider.cs - GetByBarcodeAsync, Priority, SupportedMediaTypes)
- [ ] T039 HttpClientFactory 구성 (src/CollectionServer.Api/Extensions/ServiceCollectionExtensions.cs - 각 외부 API별 HttpClient 등록)
- [ ] T040 외부 API 설정 모델 생성 (src/CollectionServer.Core/Configuration/ExternalApiSettings.cs - BaseUrl, ApiKey, Timeout 등)

### 오류 처리 및 미들웨어

- [ ] T041 [P] 커스텀 예외 클래스 생성 (src/CollectionServer.Core/Exceptions/ - InvalidBarcodeException, MediaNotFoundException, RateLimitExceededException, ExternalApiException)
- [ ] T042 ErrorHandlingMiddleware 생성 (src/CollectionServer.Api/Middleware/ErrorHandlingMiddleware.cs - 전역 예외 처리, HTTP 상태 코드 매핑)
- [ ] T043 ErrorResponse DTO 생성 (src/CollectionServer.Api/DTOs/ErrorResponse.cs - code, message, details 구조)

### 로깅 및 모니터링

- [ ] T044 Serilog 설정 (src/CollectionServer.Api/Program.cs - Console 및 File sink, JSON 포맷, 구조화된 로깅)
- [ ] T045 Health Check 엔드포인트 구현 (src/CollectionServer.Api/Endpoints/HealthEndpoint.cs - GET /health, 데이터베이스 연결 상태 확인)

### API 문서화

- [ ] T046 Swagger/OpenAPI 설정 (src/CollectionServer.Api/Program.cs - Swashbuckle 구성, contracts/openapi.yaml 기반)
- [ ] T047 OpenAPI 메타데이터 추가 (제목, 설명, 버전, 연락처, 라이선스)

### Dependency Injection 및 시작 구성

- [ ] T048 ServiceCollectionExtensions 생성 (src/CollectionServer.Api/Extensions/ServiceCollectionExtensions.cs - DI 컨테이너 구성 헬퍼 메서드)
- [ ] T049 Program.cs 기본 구성 (src/CollectionServer.Api/Program.cs - Minimal API, 미들웨어 파이프라인, DI 등록)

**Checkpoint**: 기반 인프라 완성 - 사용자 스토리 구현 시작 가능

---

## Phase 3: 사용자 스토리 1 - 개발자의 미디어 조회 통합 (우선순위: P1) 🎯 MVP

**목표**: 개발자가 단일 엔드포인트로 바코드를 제공하여 모든 미디어 타입(도서, Blu-ray/DVD, 음악)의 정보를 조회할 수 있도록 합니다.

**독립적 테스트**: 유효한 바코드로 GET /items/{barcode} 호출 시 200 OK와 완전한 JSON 응답 수신

**예상 시간**: 20-24시간

### 테스트 (US1)

- [ ] T050 [P] [US1] 단위 테스트: BarcodeValidator 검증 (tests/CollectionServer.UnitTests/Validators/BarcodeValidatorTests.cs - ISBN-10/13, UPC, EAN-13 체크섬 테스트)
- [ ] T051 [P] [US1] 단위 테스트: BarcodeDetectionService (tests/CollectionServer.UnitTests/Services/BarcodeDetectionServiceTests.cs - 형식 자동 감지 테스트)
- [ ] T052 [P] [US1] 계약 테스트: GET /items/{barcode} OpenAPI 스키마 검증 (tests/CollectionServer.ContractTests/Contracts/OpenApiSchemaTests.cs - 응답 스키마 일치 확인)

### 외부 API Provider 구현 (US1)

- [ ] T053 [P] [US1] GoogleBooksProvider 구현 (src/CollectionServer.Core/Services/Providers/GoogleBooksProvider.cs - Google Books API v1 통합, ISBN 조회, Book 엔티티 매핑)
- [ ] T054 [P] [US1] KakaoBookProvider 구현 (src/CollectionServer.Core/Services/Providers/KakaoBookProvider.cs - Kakao Book Search API 통합, Book 엔티티 매핑)
- [ ] T055 [P] [US1] AladinProvider 구현 (src/CollectionServer.Core/Services/Providers/AladinProvider.cs - Aladin API 통합, Book 엔티티 매핑)
- [ ] T056 [P] [US1] TMDbProvider 구현 (src/CollectionServer.Core/Services/Providers/TMDbProvider.cs - The Movie Database API 통합, UPC로 영화 조회, Movie 엔티티 매핑)
- [ ] T057 [P] [US1] OMDbProvider 구현 (src/CollectionServer.Core/Services/Providers/OMDbProvider.cs - OMDb API 통합, Movie 엔티티 매핑)
- [ ] T058 [P] [US1] MusicBrainzProvider 구현 (src/CollectionServer.Core/Services/Providers/MusicBrainzProvider.cs - MusicBrainz API 통합, UPC로 앨범 조회, MusicAlbum 엔티티 매핑)
- [ ] T059 [P] [US1] DiscogsProvider 구현 (src/CollectionServer.Core/Services/Providers/DiscogsProvider.cs - Discogs API 통합, MusicAlbum 엔티티 매핑)

### 핵심 서비스 로직 (US1)

- [ ] T060 [US1] IMediaService 인터페이스 정의 (src/CollectionServer.Core/Interfaces/IMediaService.cs - GetMediaByBarcodeAsync)
- [ ] T061 [US1] MediaService 구현 (src/CollectionServer.Core/Services/MediaService.cs - Database-First 로직: 1. DB 조회, 2. 외부 API 우선순위 폴백, 3. DB 저장)
- [ ] T062 [US1] 동시 요청 중복 외부 API 호출 방지 로직 구현 (MediaService 내 세마포어 또는 락 메커니즘)

### API 엔드포인트 및 DTO (US1)

- [ ] T063 [P] [US1] MediaItemResponse DTO 생성 (src/CollectionServer.Api/DTOs/MediaItemResponse.cs - 기본 필드)
- [ ] T064 [P] [US1] BookResponse DTO 생성 (src/CollectionServer.Api/DTOs/BookResponse.cs - MediaItemResponse 확장)
- [ ] T065 [P] [US1] MovieResponse DTO 생성 (src/CollectionServer.Api/DTOs/MovieResponse.cs - MediaItemResponse 확장)
- [ ] T066 [P] [US1] MusicAlbumResponse DTO 생성 (src/CollectionServer.Api/DTOs/MusicAlbumResponse.cs - MediaItemResponse 확장)
- [ ] T067 [US1] ItemsEndpoint 구현 (src/CollectionServer.Api/Endpoints/ItemsEndpoint.cs - GET /items/{barcode}, MediaService 호출, DTO 매핑)
- [ ] T068 [US1] Program.cs에 ItemsEndpoint 라우팅 등록

### 통합 테스트 (US1)

- [ ] T069 [P] [US1] 통합 테스트: 도서 조회 E2E (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 유효한 ISBN-13으로 도서 정보 조회, 200 응답 검증)
- [ ] T070 [P] [US1] 통합 테스트: 영화 조회 E2E (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 유효한 UPC로 영화 정보 조회)
- [ ] T071 [P] [US1] 통합 테스트: 음악 앨범 조회 E2E (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 유효한 UPC로 음악 앨범 정보 조회)
- [ ] T072 [US1] 통합 테스트: Database-First 캐싱 검증 (동일 바코드 재요청 시 응답 시간 < 50ms, 외부 API 호출 없음)

**Checkpoint**: 사용자 스토리 1 완전 기능, 독립적 테스트 가능, MVP 배포 준비 완료

---

## Phase 4: 사용자 스토리 2 - 최종 사용자의 미디어 발견 (우선순위: P1)

**목표**: 최종 사용자가 바코드 스캔 또는 수동 입력으로 미디어 정보를 발견하고 완전한 메타데이터를 받을 수 있도록 합니다.

**독립적 테스트**: 다양한 미디어 타입의 바코드로 조회 시 모든 관련 필드(제목, 저자/감독/아티스트, 이미지, 설명 등)가 정확하게 반환됨

**예상 시간**: 8-12시간

**Note**: 이 스토리는 US1의 기능을 기반으로 데이터 품질과 완전성에 초점을 맞춥니다.

### 데이터 품질 개선 (US2)

- [ ] T073 [P] [US2] 외부 API 응답 검증 로직 추가 (src/CollectionServer.Core/Services/Providers/ - 각 Provider에 필수 필드 검증)
- [ ] T074 [US2] 불완전한 데이터 처리 로직 (MediaService 내 여러 소스 데이터 병합 전략)
- [ ] T075 [P] [US2] 이미지 URL 유효성 검증 헬퍼 (src/CollectionServer.Core/Helpers/ImageUrlValidator.cs - URL 형식 검증, 접근 가능 여부 확인)

### DTO 확장 및 필드 매핑 (US2)

- [ ] T076 [P] [US2] DTO 매핑 확장: 저자 배열 처리 (BookResponse - 여러 저자 지원)
- [ ] T077 [P] [US2] DTO 매핑 확장: 출연진 배열 처리 (MovieResponse - 주요 출연진 목록)
- [ ] T078 [P] [US2] DTO 매핑 확장: 트랙 목록 처리 (MusicAlbumResponse - 디스크 번호, 트랙 번호, 제목, 재생 시간)

### 테스트 (US2)

- [ ] T079 [P] [US2] 단위 테스트: 여러 저자 도서 데이터 매핑 (tests/CollectionServer.UnitTests/DTOs/BookResponseTests.cs)
- [ ] T080 [P] [US2] 단위 테스트: 긴 출연진 목록 처리 (tests/CollectionServer.UnitTests/DTOs/MovieResponseTests.cs)
- [ ] T081 [P] [US2] 단위 테스트: 멀티디스크 앨범 트랙 목록 (tests/CollectionServer.UnitTests/DTOs/MusicAlbumResponseTests.cs)
- [ ] T082 [P] [US2] 통합 테스트: 표지 이미지 URL 반환 검증 (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 이미지 URL이 null이 아니고 접근 가능)
- [ ] T083 [P] [US2] 통합 테스트: 누락된 필드 null 처리 (표지 이미지 없는 미디어 조회 시 null 반환, 오류 없음)

### 데이터 보완 로직 (US2)

- [ ] T084 [US2] 우선순위 소스에서 데이터 불완전 시 다음 소스로 보완 로직 구현 (MediaService - 병합 전략)

**Checkpoint**: 최종 사용자 경험 개선, 완전한 메타데이터 제공, US1 + US2 독립적 기능 확인

---

## Phase 5: 사용자 스토리 3 - 오류 처리 및 우아한 폴백 (우선순위: P1)

**목표**: 사용자에게 명확하고 실행 가능한 오류 메시지를 제공하여 문제를 이해하고 수정할 수 있도록 합니다.

**독립적 테스트**: 잘못된 바코드, 존재하지 않는 미디어, 서비스 장애 시나리오에서 적절한 HTTP 상태 코드와 오류 메시지 반환

**예상 시간**: 6-8시간

### 오류 케이스 처리 (US3)

- [ ] T085 [P] [US3] 잘못된 바코드 형식 검증 및 400 응답 (ItemsEndpoint - BarcodeValidator 사용, ErrorResponse 반환)
- [ ] T086 [P] [US3] 미디어 미발견 시 404 응답 (MediaService - 모든 Provider 실패 후 MediaNotFoundException)
- [ ] T087 [P] [US3] 모든 외부 API 실패 시 503 응답 (MediaService - ExternalApiException, 실패한 소스 목록 포함)
- [ ] T088 [US3] 체크 디지트 오류 바코드 검증 및 구체적 오류 메시지 (BarcodeValidator - 상세 검증 오류 메시지)

### 오류 응답 개선 (US3)

- [ ] T089 [US3] ErrorResponse details 필드 확장 (src/CollectionServer.Api/DTOs/ErrorResponse.cs - provided, expectedFormats, sourcesChecked 등)
- [ ] T090 [P] [US3] 바코드 형식 오류 시 예상 형식 안내 (ErrorHandlingMiddleware - InvalidBarcodeException 처리)

### 테스트 (US3)

- [ ] T091 [P] [US3] 단위 테스트: 잘못된 바코드 형식 검증 (tests/CollectionServer.UnitTests/Validators/BarcodeValidatorTests.cs - 5자리, 글자 포함, 체크섬 오류 등)
- [ ] T092 [P] [US3] 통합 테스트: 400 Bad Request 응답 (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 잘못된 바코드로 요청, 오류 메시지 검증)
- [ ] T093 [P] [US3] 통합 테스트: 404 Not Found 응답 (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 유효하지만 존재하지 않는 바코드)
- [ ] T094 [P] [US3] 통합 테스트: 503 Service Unavailable (tests/CollectionServer.IntegrationTests/Api/ItemsEndpointTests.cs - 외부 API 모킹하여 모두 실패 시뮬레이션)

### 로깅 개선 (US3)

- [ ] T095 [US3] 오류 케이스별 구조화된 로그 추가 (MediaService, Providers - Warning/Error 레벨, 컨텍스트 정보 포함)

**Checkpoint**: 강력한 오류 처리, 명확한 사용자 피드백, US1 + US2 + US3 독립적 기능 확인

---

## Phase 6: 사용자 스토리 4 - Database-First 아키텍처를 통한 성능 최적화 (우선순위: P1)

**목표**: Database-First 전략으로 외부 API 호출을 최소화하고 응답 시간을 개선합니다.

**독립적 테스트**: 동일 바코드 재요청 시 데이터베이스에서 < 50ms 응답, 외부 API 호출 없음

**예상 시간**: 8-10시간

**Note**: US1에서 이미 기본 Database-First 로직 구현, 이 스토리는 최적화와 성능 검증에 초점

### 성능 최적화 (US4)

- [ ] T096 [P] [US4] EF Core Compiled Queries 적용 (MediaRepository - GetByBarcodeAsync 컴파일된 쿼리)
- [ ] T097 [P] [US4] AsNoTracking 최적화 (MediaRepository - 읽기 전용 쿼리)
- [ ] T098 [US4] 인덱스 성능 검증 및 쿼리 플랜 분석 (PostgreSQL EXPLAIN ANALYZE)
- [ ] T099 [US4] 동시 요청 중복 방지 락 메커니즘 최적화 (MediaService - SemaphoreSlim 또는 분산 락)

### 캐싱 전략 (US4)

- [ ] T100 [US4] 데이터베이스 캐싱 효과 모니터링 로직 추가 (MediaService - DB 히트 vs 외부 API 호출 메트릭)
- [ ] T101 [P] [US4] CreatedAt/UpdatedAt 자동 설정 검증 (MediaRepository - 타임스탬프 정확성)

### 성능 테스트 (US4)

- [ ] T102 [P] [US4] 성능 테스트: 데이터베이스 히트 응답 시간 (tests/CollectionServer.IntegrationTests/Performance/DatabasePerformanceTests.cs - p95 < 50ms 검증)
- [ ] T103 [P] [US4] 성능 테스트: 외부 API 초기 조회 응답 시간 (tests/CollectionServer.IntegrationTests/Performance/ExternalApiPerformanceTests.cs - p95 < 2초 검증)
- [ ] T104 [P] [US4] 성능 테스트: 동시 요청 처리 (tests/CollectionServer.IntegrationTests/Performance/ConcurrencyTests.cs - 동일 바코드 100개 동시 요청, 외부 API 1회만 호출)
- [ ] T105 [US4] 부하 테스트: 1000 req/s 처리 (캐시된 요청 기준, 외부 도구 사용)

### 모니터링 (US4)

- [ ] T106 [US4] 응답 시간 로깅 추가 (Middleware - 각 요청의 처리 시간 기록)
- [ ] T107 [US4] 외부 API 호출 빈도 로깅 (Providers - 호출 횟수 및 소스별 성공률)

**Checkpoint**: 성능 목표 달성, Database-First 효과 검증, US1-US4 독립적 기능 확인

---

## Phase 7: 사용자 스토리 5 - 외부 데이터 소스 우선순위 및 폴백 (우선순위: P1)

**목표**: 우선순위 기반 폴백 메커니즘으로 높은 데이터 가용성과 품질을 보장합니다.

**독립적 테스트**: 특정 외부 API 실패 시뮬레이션 시 자동으로 다음 우선순위 소스로 폴백, 최종적으로 데이터 반환 또는 404

**예상 시간**: 6-8시간

**Note**: US1에서 기본 폴백 로직 구현, 이 스토리는 우선순위 정확성과 폴백 메커니즘 검증에 초점

### 우선순위 관리 (US5)

- [ ] T108 [US5] Provider 우선순위 설정 검증 (각 Provider의 Priority 속성 확인: GoogleBooks=1, Kakao=2, Aladin=3, TMDb=1, OMDb=2, MusicBrainz=1, Discogs=2)
- [ ] T109 [US5] MediaService 폴백 로직 검증 (우선순위 순서대로 Provider 시도, 첫 성공 시 중단)

### 폴백 시나리오 테스트 (US5)

- [ ] T110 [P] [US5] 단위 테스트: 첫 번째 Provider 실패 시 두 번째 시도 (tests/CollectionServer.UnitTests/Services/MediaServiceTests.cs - Moq로 GoogleBooks 실패, Kakao 성공 모킹)
- [ ] T111 [P] [US5] 단위 테스트: 모든 Provider 실패 시 404 (tests/CollectionServer.UnitTests/Services/MediaServiceTests.cs - 모든 Provider null 반환)
- [ ] T112 [P] [US5] 통합 테스트: 도서 우선순위 폴백 (tests/CollectionServer.IntegrationTests/Api/FallbackTests.cs - GoogleBooks 모킹 실패, Kakao에서 데이터 수신)
- [ ] T113 [P] [US5] 통합 테스트: 영화 우선순위 폴백 (tests/CollectionServer.IntegrationTests/Api/FallbackTests.cs - TMDb 실패, OMDb 성공)
- [ ] T114 [P] [US5] 통합 테스트: 음악 우선순위 폴백 (tests/CollectionServer.IntegrationTests/Api/FallbackTests.cs - MusicBrainz 실패, Discogs 성공)

### 불완전 데이터 처리 (US5)

- [ ] T115 [US5] Provider 응답 완전성 점수 로직 구현 (각 Provider - 필수/선택 필드 채움 비율 계산)
- [ ] T116 [US5] 불완전한 데이터 시 다음 소스 시도 옵션 구현 (MediaService - 완전성 임계값 설정)

### 로깅 및 모니터링 (US5)

- [ ] T117 [US5] 폴백 발생 시 로깅 추가 (MediaService - 실패한 Provider 및 시도 순서 기록)
- [ ] T118 [US5] 외부 API 성공률 메트릭 추가 (Providers - 소스별 호출 성공/실패 비율)

**Checkpoint**: 강력한 폴백 메커니즘, 높은 데이터 가용성, US1-US5 독립적 기능 확인

---

## Phase 8: 사용자 스토리 6 - 속도 제한을 통한 공정한 API 사용 (우선순위: P2)

**목표**: 속도 제한을 구현하여 악용을 방지하고 모든 사용자에게 공정한 접근을 보장합니다.

**독립적 테스트**: 속도 제한을 초과하는 요청 시 429 Too Many Requests 응답 및 Retry-After 헤더 반환

**예상 시간**: 4-6시간

### Rate Limiting 구현 (US6)

- [ ] T119 [US6] ASP.NET Core Rate Limiter 구성 (src/CollectionServer.Api/Program.cs - FixedWindowLimiter, 100 req/min)
- [ ] T120 [US6] ItemsEndpoint에 Rate Limiting 적용 (RequireRateLimiting 속성 추가)
- [ ] T121 [US6] 429 응답 시 Retry-After 헤더 추가 (RateLimiting 미들웨어 커스터마이징)

### 테스트 (US6)

- [ ] T122 [P] [US6] 단위 테스트: Rate Limiter 설정 검증 (tests/CollectionServer.UnitTests/Middleware/RateLimitingTests.cs - 윈도우 크기, 제한 수 확인)
- [ ] T123 [P] [US6] 통합 테스트: 속도 제한 내 요청 정상 처리 (tests/CollectionServer.IntegrationTests/Api/RateLimitingTests.cs - 99개 요청 모두 200 응답)
- [ ] T124 [P] [US6] 통합 테스트: 속도 제한 초과 시 429 응답 (tests/CollectionServer.IntegrationTests/Api/RateLimitingTests.cs - 101번째 요청 429, Retry-After 헤더 확인)
- [ ] T125 [US6] 통합 테스트: 속도 제한 윈도우 리셋 검증 (1분 대기 후 다시 요청 가능)

### 설정 및 문서화 (US6)

- [ ] T126 [P] [US6] Rate Limiting 설정 appsettings.json에 외부화 (PermitLimit, Window 구성 가능)
- [ ] T127 [P] [US6] Rate Limiting 정책 API 문서에 추가 (contracts/openapi.yaml - 429 응답 예제)

**Checkpoint**: 속도 제한 작동, 악용 방지, US1-US6 독립적 기능 확인

---

## Phase 9: 마무리 및 교차 관심사 (Sprint 3)

**목적**: 모든 사용자 스토리에 영향을 미치는 개선사항

**예상 시간**: 12-16시간

### 문서화

- [ ] T128 [P] README.md 작성 (리포지토리 루트 - 프로젝트 개요, 기술 스택, 빠른 시작)
- [ ] T129 [P] API 사용 가이드 작성 (docs/api-guide.md - 엔드포인트, 요청/응답 예제, 오류 코드)
- [ ] T130 [P] 배포 가이드 작성 (docs/deployment.md - Docker, Kubernetes, 환경 변수 설정)
- [ ] T131 [P] 외부 API 키 발급 가이드 작성 (docs/external-apis.md - 각 API 키 발급 방법 및 제한 사항)

### 코드 품질

- [ ] T132 [P] 코드 주석 및 XML 문서화 추가 (공개 API 인터페이스 및 주요 클래스)
- [ ] T133 [P] 린트 및 코드 스타일 검증 (dotnet format 실행, 경고 수정)
- [ ] T134 리팩토링: 중복 코드 제거 및 DRY 원칙 적용

### 보안 강화

- [ ] T135 [P] User Secrets 사용 검증 (개발 환경에서 appsettings.json에 API 키 노출되지 않도록)
- [ ] T136 [P] HTTPS 강제 설정 (프로덕션 환경, Program.cs - UseHttpsRedirection)
- [ ] T137 [P] CORS 정책 구성 (필요 시, Program.cs - AddCors)
- [ ] T138 SQL Injection 방지 검증 (EF Core 파라미터화된 쿼리 사용 확인)

### 성능 최적화

- [ ] T139 [P] 응답 압축 활성화 (Program.cs - AddResponseCompression, gzip/brotli)
- [ ] T140 [P] HTTP/2 지원 활성화 (Kestrel 설정)

### CI/CD 파이프라인

- [ ] T141 GitHub Actions 워크플로우 생성 (.github/workflows/ci.yml - 빌드, 테스트, 린트)
- [ ] T142 Docker 이미지 빌드 및 푸시 워크플로우 (.github/workflows/docker-publish.yml - GitHub Container Registry 또는 Docker Hub)
- [ ] T143 [P] 프로덕션 배포 스크립트 (scripts/deploy.sh - 환경 변수 주입, 마이그레이션 적용, 서비스 시작)

### 모니터링 및 관찰성

- [ ] T144 [P] 메트릭 엔드포인트 추가 (GET /metrics - Prometheus 형식, 요청 카운트, 응답 시간, 오류율)
- [ ] T145 [P] 분산 추적 설정 (OpenTelemetry 또는 Application Insights - 요청 흐름 추적)

### 최종 검증

- [ ] T146 quickstart.md 가이드 완전 실행 검증 (새 환경에서 처음부터 설정, 첫 API 요청까지)
- [ ] T147 전체 테스트 스위트 실행 (dotnet test - 모든 단위, 통합, 계약 테스트 통과)
- [ ] T148 OpenAPI 스키마 검증 (contracts/openapi.yaml과 실제 API 응답 일치 확인)
- [ ] T149 성능 벤치마크 실행 (데이터베이스 히트 < 50ms, 외부 API < 2초 목표 달성 확인)

### 추가 기능 (선택 사항)

- [ ] T150 [P] 헬스 체크 확장 (외부 API 연결 상태 포함)
- [ ] T151 [P] 관리자 엔드포인트 (캐시 클리어, 통계 조회 - 인증 필요)

**Checkpoint**: 프로덕션 준비 완료, 문서화 완성, 모든 테스트 통과

---

## 의존성 및 실행 순서

### 페이즈 의존성

- **Phase 1 (프로젝트 설정)**: 의존성 없음 - 즉시 시작 가능
- **Phase 2 (기반 인프라)**: Phase 1 완료 필요 - **모든 사용자 스토리 차단**
- **Phase 3-8 (사용자 스토리)**: Phase 2 완료 필요
  - 사용자 스토리는 병렬 진행 가능 (팀 용량 허용 시)
  - 또는 우선순위 순서대로 순차 진행 (P1 → P2)
- **Phase 9 (마무리)**: 모든 원하는 사용자 스토리 완료 후

### 사용자 스토리 의존성

- **US1 (개발자 미디어 조회)**: Phase 2 완료 후 시작 - 다른 스토리 의존성 없음
- **US2 (최종 사용자 발견)**: Phase 2 완료 후 시작 - US1과 병렬 가능하나 US1 기능 기반 확장
- **US3 (오류 처리)**: Phase 2 완료 후 시작 - US1 기본 엔드포인트 있어야 함
- **US4 (성능 최적화)**: Phase 2 완료 후 시작 - US1 Database-First 로직 구현 필요
- **US5 (우선순위 폴백)**: Phase 2 완료 후 시작 - US1 폴백 로직 구현 필요
- **US6 (속도 제한)**: Phase 2 완료 후 시작 - 독립적, US1 엔드포인트만 필요

### 각 사용자 스토리 내

- 테스트 먼저 작성 → 실패 확인 → 구현 → 테스트 통과
- 모델 → 서비스 → 엔드포인트 순서
- 핵심 구현 → 통합 → 독립적 검증

### 병렬 실행 기회

- Phase 1: T003-T010 (프로젝트 생성), T011-T013 (패키지 설치), T015-T019 (환경 설정) 모두 병렬 가능
- Phase 2: T022-T024 (엔티티), T027-T030 (Configuration), T041 (예외), 각 그룹 내 병렬 가능
- Phase 3 (US1): T050-T052 (테스트), T053-T059 (Providers), T063-T066 (DTO), T069-T071 (통합 테스트) 병렬 가능
- Phase 2 완료 후 US1, US2, US3, US4, US5, US6를 다른 개발자가 동시 진행 가능

---

## Phase별 병렬 실행 예제

### Phase 1: 프로젝트 설정

```bash
# 병렬 실행 가능:
T003: API 프로젝트 생성
T004: Core 프로젝트 생성
T005: Infrastructure 프로젝트 생성
T006-T008: 테스트 프로젝트들 생성
```

### Phase 2: 기반 인프라

```bash
# 엔티티 생성 (병렬):
T022: Book 엔티티
T023: Movie 엔티티
T024: MusicAlbum 엔티티

# EF Core Configuration (병렬):
T027: BookConfiguration
T028: MovieConfiguration
T029: MusicAlbumConfiguration
```

### Phase 3: 사용자 스토리 1

```bash
# Provider 구현 (병렬 - 7개 작업):
T053: GoogleBooksProvider
T054: KakaoBookProvider
T055: AladinProvider
T056: TMDbProvider
T057: OMDbProvider
T058: MusicBrainzProvider
T059: DiscogsProvider

# DTO 생성 (병렬):
T064: BookResponse
T065: MovieResponse
T066: MusicAlbumResponse
```

---

## 구현 전략

### MVP 우선 (사용자 스토리 1만)

1. Phase 1: 프로젝트 설정 완료
2. Phase 2: 기반 인프라 완료 (**중요 - 모든 스토리 차단**)
3. Phase 3: 사용자 스토리 1 완료
4. **중단 및 검증**: US1 독립적 테스트
5. 필요 시 배포/데모

### 점진적 전달

1. Setup + Foundational → 기반 준비
2. US1 추가 → 독립 테스트 → 배포/데모 (MVP!)
3. US2 추가 → 독립 테스트 → 배포/데모
4. US3 추가 → 독립 테스트 → 배포/데모
5. US4 추가 → 독립 테스트 → 배포/데모
6. US5 추가 → 독립 테스트 → 배포/데모
7. US6 추가 → 독립 테스트 → 배포/데모
8. 각 스토리가 이전 스토리를 손상시키지 않고 가치 추가

### 병렬 팀 전략

여러 개발자가 있는 경우:

1. 팀이 Setup + Foundational을 함께 완료
2. Foundational 완료 후:
   - 개발자 A: 사용자 스토리 1 (우선순위)
   - 개발자 B: 사용자 스토리 2
   - 개발자 C: 사용자 스토리 3
   - 개발자 D: 사용자 스토리 6 (독립적)
3. 스토리들이 독립적으로 완료 및 통합

---

## 요약

### 총 작업 수: 151개 작업

### 사용자 스토리별 작업 수

- **Phase 1 (설정)**: 19개 작업 (4-6시간)
- **Phase 2 (기반 인프라)**: 29개 작업 (12-16시간) - **모든 스토리 차단**
- **Phase 3 (US1)**: 23개 작업 (20-24시간) 🎯 **MVP**
- **Phase 4 (US2)**: 12개 작업 (8-12시간)
- **Phase 5 (US3)**: 11개 작업 (6-8시간)
- **Phase 6 (US4)**: 12개 작업 (8-10시간)
- **Phase 7 (US5)**: 11개 작업 (6-8시간)
- **Phase 8 (US6)**: 9개 작업 (4-6시간)
- **Phase 9 (마무리)**: 25개 작업 (12-16시간)

### 병렬 실행 기회

- Phase 1: 16개 작업 병렬 가능 ([P] 태그)
- Phase 2: 17개 작업 병렬 가능
- Phase 3 (US1): 16개 작업 병렬 가능
- Phase 4-8: 각 사용자 스토리 내 테스트, DTO, Provider 병렬 가능
- **Phase 2 완료 후 모든 사용자 스토리(US1-US6) 병렬 진행 가능**

### 독립적 테스트 기준

- **US1**: 유효한 바코드로 API 호출 → 200 OK + 완전한 JSON 응답
- **US2**: 다양한 미디어 타입 조회 → 모든 메타데이터 필드 정확히 반환
- **US3**: 잘못된 입력/미발견/장애 → 적절한 HTTP 상태 + 명확한 오류 메시지
- **US4**: 재요청 → <50ms 응답 (DB 히트), 외부 API 호출 없음
- **US5**: 첫 API 실패 → 자동 폴백 → 최종 데이터 반환 또는 404
- **US6**: 101번째 요청 → 429 Too Many Requests + Retry-After 헤더

### 제안 MVP 범위

**MVP = Phase 1 + Phase 2 + Phase 3 (US1)**

- 프로젝트 설정 및 기반 인프라
- 기본 미디어 조회 기능
- 7개 외부 API 통합
- Database-First 캐싱
- 기본 오류 처리
- Swagger/OpenAPI 문서

**예상 시간**: 36-46시간 (1-2주, 1-2명)

이후 US2-US6를 점진적으로 추가하여 기능 확장

---

## 형식 검증

✅ **모든 작업이 체크리스트 형식을 따릅니다**: `- [ ] [ID] [P?] [Story?] Description with file path`

✅ **사용자 스토리 레이블이 적절히 적용되었습니다**: Phase 3-8의 모든 작업에 [US1]-[US6] 레이블

✅ **병렬 실행 가능 작업에 [P] 태그 표시**: 다른 파일, 의존성 없는 작업들

✅ **파일 경로가 설명에 포함됨**: 각 작업에 정확한 파일 경로 명시

✅ **독립적 테스트 기준 명시**: 각 사용자 스토리마다 검증 방법 제공

---

**tasks.md 생성 완료** ✅

문의사항: support@collectionserver.example
