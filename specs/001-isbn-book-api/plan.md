# Implementation Plan: 미디어 정보 API 서버

**Branch**: `001-isbn-book-api` | **Date**: 2025-11-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-isbn-book-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

다양한 미디어 유형(도서, Blu-ray, DVD, 음악 앨범)의 바코드/ISBN을 입력받아 해당 미디어의 상세 정보를 반환하는 통합 REST API 서버를 개발합니다. Database-First 아키텍처를 사용하여 PostgreSQL 데이터베이스를 먼저 확인하고, 없을 경우 우선순위에 따라 외부 데이터 소스를 조회한 후 결과를 저장합니다. .NET 10.0, C# 13, ASP.NET Core 10.0, Entity Framework Core 10.0을 기술 스택으로 사용합니다.

## Technical Context

**Language/Version**: C# 13 (.NET 10.0)  
**Framework**: ASP.NET Core 10.0 (REST API with minimal APIs)  
**Primary Dependencies**: 
- Entity Framework Core 10.0 (ORM, Database-First pattern)
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0 (PostgreSQL provider)
- Microsoft.AspNetCore.OpenApi 10.0 (OpenAPI/Swagger support)
- Swashbuckle.AspNetCore 7.0.0 (API documentation)
- Serilog.AspNetCore 10.0 (Structured logging)
- HttpClientFactory (외부 API 호출)

**Storage**: PostgreSQL 16+ (미디어 메타데이터, 바코드 인덱싱, 캐시 데이터)  
**Testing**: xUnit + Moq + FluentAssertions (단위, 통합, 계약 테스트)  
**Target Platform**: Linux/Windows server (Docker 컨테이너, Kubernetes 배포)  
**Project Type**: 단일 웹 API 프로젝트 (백엔드 전용 REST API)  
**Performance Goals**: 
- 데이터베이스 히트: <50ms p95
- 외부 API 폴백: <2초 p95
- 1000 req/s 처리 (캐시된 요청 기준)
- Database-First 아키텍처로 외부 API 호출 최소화

**Constraints**: 
- 외부 API 속도 제한 준수 (각 API별 제한)
- 바코드 형식 검증 필수 (ISBN-10/13, UPC, EAN-13)
- 오류 시 명확한 HTTP 상태 코드 반환
- 동시 요청 중복 외부 API 호출 방지

**Scale/Scope**: 
- 4가지 미디어 유형 (도서, Blu-ray, DVD, 음악)
- 7개 외부 데이터 소스 통합
- 우선순위 기반 폴백 메커니즘
- 단일 통합 엔드포인트 (GET /items/{barcode})

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ✅ PASS - 프로젝트가 constitution 원칙을 준수하고 있습니다.

| Gate | Status | Notes |
|------|--------|-------|
| 단순성 (Simplicity) | ✅ PASS | 단일 API 프로젝트, 명확한 책임, Database-First 패턴으로 복잡도 관리 |
| 테스트 가능성 (Testability) | ✅ PASS | xUnit 기반 단위/통합/계약 테스트, 외부 API 모킹 가능 |
| 독립성 (Independence) | ✅ PASS | 자체 포함된 REST API, PostgreSQL 의존성만 필수 |
| 문서화 (Documentation) | ✅ PASS | OpenAPI/Swagger 자동 생성, 명세서 기반 개발 |
| 성능 (Performance) | ✅ PASS | Database-First 캐싱 전략, 명확한 성능 목표 설정 |

**복잡도 정당성**: 해당 없음 - 모든 게이트 통과

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── CollectionServer.Api/
│   ├── Program.cs                      # .NET 10 minimal API 진입점
│   ├── appsettings.json                # 설정 (연결 문자열, API 키)
│   ├── appsettings.Development.json
│   ├── CollectionServer.Api.csproj     # <TargetFramework>net10.0</TargetFramework>
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs  # DI 설정
│   ├── Middleware/
│   │   ├── RateLimitingMiddleware.cs   # 속도 제한
│   │   └── ErrorHandlingMiddleware.cs  # 전역 예외 처리
│   └── Endpoints/
│       └── ItemsEndpoint.cs            # GET /items/{barcode}
│
├── CollectionServer.Core/
│   ├── CollectionServer.Core.csproj    # <TargetFramework>net10.0</TargetFramework>
│   ├── Models/
│   │   ├── MediaItem.cs                # 통합 미디어 엔티티
│   │   ├── Book.cs                     # 도서 특화 필드
│   │   ├── Movie.cs                    # Blu-ray/DVD 필드
│   │   └── MusicAlbum.cs               # 음악 앨범 필드
│   ├── Services/
│   │   ├── IMediaService.cs            # 메인 서비스 인터페이스
│   │   ├── MediaService.cs             # Database-First 로직
│   │   ├── BarcodeDetectionService.cs  # 바코드 형식 감지
│   │   └── Providers/
│   │       ├── IMediaProvider.cs       # 외부 API 추상화
│   │       ├── GoogleBooksProvider.cs
│   │       ├── KakaoBookProvider.cs
│   │       ├── AladinProvider.cs
│   │       ├── TMDbProvider.cs
│   │       ├── OMDbProvider.cs
│   │       ├── MusicBrainzProvider.cs
│   │       └── DiscogsProvider.cs
│   └── Validators/
│       └── BarcodeValidator.cs         # ISBN/UPC/EAN 검증
│
├── CollectionServer.Infrastructure/
│   ├── CollectionServer.Infrastructure.csproj  # <TargetFramework>net10.0</TargetFramework>
│   ├── Data/
│   │   ├── ApplicationDbContext.cs     # EF Core 10.0 DbContext
│   │   ├── Configurations/
│   │   │   ├── MediaItemConfiguration.cs
│   │   │   ├── BookConfiguration.cs
│   │   │   ├── MovieConfiguration.cs
│   │   │   └── MusicAlbumConfiguration.cs
│   │   └── Migrations/                 # EF Core 마이그레이션
│   └── Repositories/
│       ├── IMediaRepository.cs
│       └── MediaRepository.cs          # 데이터베이스 접근 레이어
│
└── global.json                         # SDK 버전: 10.0.100

tests/
├── CollectionServer.UnitTests/
│   ├── CollectionServer.UnitTests.csproj  # <TargetFramework>net10.0</TargetFramework>
│   ├── Services/
│   │   ├── MediaServiceTests.cs        # Database-First 로직 테스트
│   │   ├── BarcodeDetectionServiceTests.cs
│   │   └── Providers/
│   │       └── [각 Provider별 단위 테스트]
│   └── Validators/
│       └── BarcodeValidatorTests.cs
│
├── CollectionServer.IntegrationTests/
│   ├── CollectionServer.IntegrationTests.csproj
│   ├── Api/
│   │   └── ItemsEndpointTests.cs       # E2E API 테스트
│   └── Infrastructure/
│       └── DatabaseTests.cs            # EF Core 통합 테스트
│
└── CollectionServer.ContractTests/
    ├── CollectionServer.ContractTests.csproj
    └── Contracts/
        └── OpenApiSchemaTests.cs       # OpenAPI 계약 검증

docker/
├── Dockerfile                          # FROM mcr.microsoft.com/dotnet/sdk:10.0
└── docker-compose.yml                  # PostgreSQL + API 컨테이너
```

**Structure Decision**: 
- **Clean Architecture** 적용: API 레이어(Endpoints), Core 레이어(Business Logic), Infrastructure 레이어(Data Access) 분리
- **단일 솔루션, 여러 프로젝트**: 관심사 분리 및 테스트 가능성 향상
- **Tests 폴더 분리**: 단위, 통합, 계약 테스트를 별도 프로젝트로 구성
- **.NET 10.0 표준**: 모든 프로젝트에서 `<TargetFramework>net10.0</TargetFramework>` 사용
- **C# 13 기능 활용**: `<LangVersion>13.0</LangVersion>` 설정으로 최신 언어 기능 사용

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
