# CollectionServer – 바코드 기반 미디어 정보 API

CollectionServer는 ISBN / UPC / EAN 바코드를 입력받아 도서·영화·음반의 상세 메타데이터를 반환하는 ASP.NET Core 10 기반 REST API입니다. 캐시 → 데이터베이스 → 외부 Provider 순으로 조회하며, 우선순위·회복탄력성 정책과 한국어 ProblemDetails 오류 응답을 제공합니다.

## 주요 특징
- **Cache → DB → Provider 파이프라인**: Garnet(REDIS 호환), PostgreSQL, 우선순위가 지정된 Provider(Google Books, Kakao Book, Aladin, MusicBrainz, Discogs, UpcItemDb+TMDb)를 순차적으로 조회합니다.
- **Resilience 내장 Provider**: `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience`를 이용해 재시도·서킷브레이커·타임아웃을 표준화하고 Serilog 구조화 로그로 기록합니다.
- **Rate Limiting & ProblemDetails**: 고정 창(개발 100 req/min, 운영 200 req/min) 제한과 한국어 ProblemDetails를 제공하여 예측 가능한 오류 처리를 지원합니다.
- **자동화된 테스트**: `dotnet test` 실행 시 단위/통합/계약 테스트 280개가 실행되어 CI 파이프라인과 동일한 품질을 보장합니다.
- **컨테이너/배포**: 멀티 스테이지 `Containerfile`, 개발용 `podman-compose`, 운영용 `docker-compose.prod.yml`, GHCR 배포 파이프라인(`.github/workflows/ci-cd.yml`)을 제공합니다.

## 기술 스택
- .NET 10.0, ASP.NET Core Minimal API, EF Core 10
- PostgreSQL 16 (운영), EF InMemory (개발/테스트), Garnet(REDIS 호환 캐시)
- Serilog, `Microsoft.Extensions.Http.Resilience`, StackExchange.Redis
- Swagger/OpenAPI, xUnit + Moq + FluentAssertions

## 디렉터리 구조
```
CollectionServer/
├── src/
│   ├── CollectionServer.Api            # Minimal API, DI, 미들웨어
│   ├── CollectionServer.Core           # 엔터티, 서비스, Validator, 인터페이스
│   └── CollectionServer.Infrastructure # EF DbContext, 리포지토리, Provider
├── tests/                              # 단위/통합/계약 테스트
├── docs/                               # API 가이드, 배포 문서
├── specs/                              # 기능 명세·설계 문서
├── podman-compose.yml                  # 개발용 컨테이너 스택
└── docker-compose.prod.yml             # 운영용 스택 (Postgres + Garnet + Nginx)
```

## Provider 커버리지 (2025-11-27 기준)
| 도메인 | Provider | 상태 | 지원 바코드 | 비고 |
| --- | --- | --- | --- | --- |
| 도서 | Google Books | ✅ 운영 준비 | ISBN-10/13 | 전 세계 메타데이터 + 표지 |
| 도서 | Kakao Book | ✅ 운영 준비 | ISBN-10/13 | 한국 도서 우선, Authorization 헤더 |
| 도서/음반/DVD | Aladin API | ✅ 운영 준비 | ISBN-10/13, UPC/EAN-13 | mallType 기반으로 Book/Music/DVD 매핑 |
| 음악 | MusicBrainz | ✅ 운영 준비 | UPC/EAN-13 | 트랙 리스트·레이블 정보 포함 |
| 음악 | Discogs | ✅ 운영 준비 | UPC/EAN-13 | 2단계 검색(바코드→릴리즈→상세) |
| 영화 | UpcItemDb + TMDb 브리지 | ✅ 운영 준비 | UPC/EAN-13 (ISBN 제외) | UPCitemdb로 제목 확보 후 TMDb 세부 정보 조회 |
| 영화 | TMDb | ⚠️ Stub | UPC/EAN-13 (ISBN 제외) | 바코드 직접 검색 미지원 → 로그 후 폴백 |
| 영화 | OMDb | ⚠️ Stub | UPC 12자리 | UPC→IMDb 매핑 도입 전까지 Stub 유지 |

Provider 우선순위는 `ExternalApis:{Provider}.Priority` 값(낮을수록 우선)에 따라 동적으로 계산됩니다. `MediaService`는 Provider 지원 여부·시도·성공 내역을 모두 로그에 남깁니다.

## 로컬 개발
### 요구 사항
- .NET SDK 10.0.100 (`global.json` 참고)
- PostgreSQL 16 (DB 연동 시 필요)
- Podman 또는 Docker (컨테이너 워크플로우 사용 시)

### 빠른 시작
```bash
git clone <repository-url>
cd CollectionServer

dotnet restore
# API 키 설정 후 실행
dotnet run --project src/CollectionServer.Api
```
개발 환경(`ASPNETCORE_ENVIRONMENT=Development`)에서는 EF InMemory + No-Op 캐시를 사용하므로 Postgres/Garnet 없이도 즉시 실행됩니다. Swagger UI는 `https://localhost:5001/swagger`(또는 `http://localhost:5000/swagger`)에서 확인할 수 있습니다.

### API 키 설정 (User Secrets)
```bash
cd src/CollectionServer.Api
dotnet user-secrets init

dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey"   "<google>"
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey"     "<kakao>"
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey"     "<aladin-ttb>"
dotnet user-secrets set "ExternalApis:Discogs:ApiKey"       "<discogs-token>"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret"    "<discogs-secret>"
dotnet user-secrets set "ExternalApis:TMDb:ApiKey"          "<tmdb>"
dotnet user-secrets set "ExternalApis:OMDb:ApiKey"          "<omdb>"
```
민감 정보는 Git에 저장하지 말고 `.env` / `.env.prod` 템플릿 또는 User Secrets로 관리하세요.

### 샘플 요청
```bash
# ISBN-13 도서 조회
curl http://localhost:5000/items/9788966262281

# 헬스 체크
curl http://localhost:5000/health
```
Rate Limiter를 초과하면 HTTP 429 + `Retry-After` 헤더 + 한국어 ProblemDetails가 반환됩니다.

## 컨테이너 워크플로우
### 개발용 (podman-compose)
```bash
podman-compose up -d
```
멀티 스테이지 `Containerfile`로 이미지를 빌드하고 Postgres/Garnet 컨테이너를 함께 띄웁니다. API는 `Development` 환경으로 동작하여 InMemory DB를 유지하면서도 컨테이너 호환성을 검증할 수 있습니다.

### 운영 유사 환경 (docker-compose.prod)
```bash
cp .env.prod.example .env.prod   # DB + API 키 입력
cp .env.example .env            # (선택) Discogs 등 추가 비밀 값

docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```
`ASPNETCORE_ENVIRONMENT=Production`으로 실행되어 PostgreSQL + Garnet + Nginx가 활성화되며, GHCR(`ghcr.io/<repo>:latest`) 이미지가 자동으로 Pull 됩니다.

## 테스트
전체 테스트 실행:
```bash
dotnet test
```
2025-11-27 기준 **280개** 테스트(Provider 단위 테스트, Mock HTTP 기반 통합 테스트, 계약 테스트)가 실행됩니다.
```bash
# Provider 테스트만 실행
dotnet test --filter FullyQualifiedName~ProviderTests

# 통합 테스트
dotnet test tests/CollectionServer.IntegrationTests
```

## 문서 & 명세
- `docs/api-guide.md` – 요청/응답 예시, 오류 코드, Rate Limiting 설명
- `docs/deployment.md` – 시스템 요구 사항, Kubernetes/Systemd 예시, 백업/복구 가이드
- `specs/001-isbn-book-api/*.md` – 기능 명세, 설계, 작업 리스트

## 배포 & 모니터링
- `.github/workflows/ci-cd.yml` – `dotnet test` → 컨테이너 빌드 → GHCR 배포 자동화
- `docker-compose.prod.yml` + `nginx/` – Nginx → API → Postgres → Garnet 파이프라인 예제
- Serilog 구조화 로그는 콘솔 및 `logs/collectionserver-*.log` 파일로 출력되어 중앙집중 로그 시스템과 연동할 수 있습니다.

## 기여 방법
1. 저장소를 Fork 후 기능 브랜치를 생성합니다.
2. 변경 전후로 `dotnet test`를 실행해 테스트를 통과시킵니다.
3. 최신 문서를 반영한 Pull Request를 제출합니다.

이슈·기능 제안은 GitHub Issues를 통해 언제든지 환영합니다.
