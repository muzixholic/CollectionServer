# CollectionServer – 바코드 기반 미디어 정보 API

CollectionServer는 ISBN / UPC / EAN 바코드를 입력받아 도서·영화·음반의 상세 메타데이터를 반환하는 ASP.NET Core 10 기반 REST API입니다. 캐시 → 데이터베이스 → 외부 Provider 순으로 조회하며, 우선순위·회복탄력성 정책과 한국어 ProblemDetails 오류 응답을 제공합니다.

## 주요 특징
- **Cache → DB → Provider 파이프라인**: Garnet(REDIS 호환), PostgreSQL, 우선순위가 지정된 Provider(Google Books, Kakao Book, Aladin, MusicBrainz, Discogs, UpcItemDb Resolver + TMDb, OMDb)를 순차적으로 조회합니다.
- **Resilience 내장 Provider**: `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience`를 이용해 재시도·서킷브레이커·타임아웃을 표준화하고 Serilog 구조화 로그로 기록합니다.
- **Rate Limiting & ProblemDetails**: 클라이언트 IP 기준 고정 창(개발 100 req/min, 운영 200 req/min) 제한과 한국어 ProblemDetails를 제공하며, 초과 시 `Retry-After`, `X-RateLimit-Limit/Remaining/Reset` 헤더와 전용 JSON 응답을 함께 반환합니다.
- **운영 모니터링**: OpenTelemetry + Prometheus를 통해 애플리케이션 메트릭(HTTP 요청, 런타임 상태 등)을 수집하고 `/metrics` 엔드포인트를 제공합니다.
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
| 영화 | UpcItemDb Resolver (서비스) | ✅ 운영 | UPC/EAN-13 (ISBN 제외) | UPCitemdb 제목/연도 → TMDb ID 캐싱, Provider에 주입 |
| 영화 | TMDb | ✅ 운영 (Resolver) | UPC/EAN-13 (ISBN 제외) | Resolver의 TMDb ID/제목으로 상세 조회 |
| 영화 | OMDb | ✅ 운영 (Resolver) | UPC 12자리 | Resolver 결과(제목/연도/이미지) 기반 OMDb 호출 |

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
mkdir -p logs                   # Serilog 파일 공유 (Promtail)

docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```
`ASPNETCORE_ENVIRONMENT=Production`으로 실행되어 PostgreSQL + Garnet + Nginx가 활성화되며, GHCR(`ghcr.io/<repo>:latest`) 이미지가 자동으로 Pull 됩니다. `./logs` 디렉터리는 컨테이너 `/app/logs`에 마운트되어 Promtail이 호스트에서 파일을 수집할 수 있습니다.

> TLS를 활성화하려면 `nginx/conf.d/default.conf`에 실제 도메인과 인증서 경로를 설정하고, Certbot을 통해 `/var/www/certbot` 웹루트를 이용해 인증서를 발급한 뒤 `docker-compose restart nginx`로 반영하세요.
> - 장기적으로는 `docker compose --profile certbot up -d certbot` 명령으로 자동 갱신 컨테이너를 실행하거나,
> - `ops/certbot-renew.workflow.yml` 템플릿을 `.github/workflows/certbot-renew.yml`로 복사해 GitHub Actions 기반 원격 `certbot renew` → `nginx` 재로드를 자동화하세요. (필수 Secrets: `SSH_HOST`, `SSH_USER`, `SSH_PRIVATE_KEY`, `SSH_PORT`(옵션), `DEPLOY_PATH`, `LETSENCRYPT_EMAIL`, `LETSENCRYPT_DOMAINS`)

### 모니터링 스택 (Prometheus / Grafana / Loki / Tempo / Alertmanager)
```bash
cd ops/monitoring
# 필요 시 alertmanager.yml / promtail-config.yml을 편집해 알림 채널·로그 경로를 조정하세요.
docker compose -f docker-compose.monitoring.yml up -d
```
- **Prometheus**: http://localhost:9090, `/metrics` (API)와 자체 상태를 수집
- **Alertmanager**: http://localhost:9093, `ops/monitoring/alertmanager.yml`에서 이메일/웹훅 설정
- **Grafana**: http://localhost:3000 (`admin/admin` 초기 비밀번호) – Prometheus/Loki/Tempo 데이터 소스 추가 후 대시보드 구성
- **Loki + Promtail**: `./logs`(Serilog 파일)을 읽어 Loki에 저장, Grafana Explore에서 조회
- **Tempo**: OTLP gRPC(4317)/HTTP(4318)를 통해 OpenTelemetry Trace 수신
- **Alertmanager (Telegram 기본)**: `ops/monitoring/alertmanager.yml` 에서 `bot_token`/`chat_id` 를 텔레그램 봇/채널 값으로 교체하세요. Slack 유료 플랜 전환 시 `slack_configs` 의 웹훅 URL과 채널을 설정하고 route 의 기본 receiver 를 `slack` 으로 변경하면 됩니다.
- **주요 메트릭**: `collectionserver_cache_hits_total`, `collectionserver_cache_misses_total`, `collectionserver_database_hits_total`, `collectionserver_provider_success_total`, `collectionserver_provider_failure_total`, `collectionserver_provider_latency_ms`, `collectionserver_rate_limit_rejections_total`

> 모니터링 스택은 메인 서비스와 동일한 `collectionserver-network`를 사용하도록 구성되어 있습니다. 네트워크가 없다면 `docker network create collectionserver-network` 후 실행하세요.

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
- `.github/workflows/ci-cd.yml` – `dotnet test` → 컨테이너 빌드 → GHCR 배포 자동화 (필요 시 `ops/ci-with-postgres.workflow.yml` 템플릿을 병합하여 PostgreSQL 마이그레이션 검증 단계를 추가)
- `ops/certbot-renew.workflow.yml` – GitHub Actions용 Certbot 파이프라인 템플릿 (복사 후 Secrets에 SSH/도메인 정보를 등록하여 사용)
- `docker-compose.prod.yml` + `nginx/` – Nginx → API → Postgres → Garnet 파이프라인 예제
- Serilog 구조화 로그는 콘솔 및 `logs/collectionserver-*.log` 파일로 출력되어 중앙집중 로그 시스템과 연동할 수 있습니다.

## 기여 방법
1. 저장소를 Fork 후 기능 브랜치를 생성합니다.
2. 변경 전후로 `dotnet test`를 실행해 테스트를 통과시킵니다.
3. 최신 문서를 반영한 Pull Request를 제출합니다.

이슈·기능 제안은 GitHub Issues를 통해 언제든지 환영합니다.
