# CollectionServer – Barcode-first Media Lookup API

CollectionServer is a clean-architecture ASP.NET Core 10 API that turns ISBN / UPC / EAN barcodes into rich metadata for books, Blu-ray / DVD titles, and music albums. The service performs cache-first, database-first lookups before fanning out to prioritized external providers with resilience policies and Korean-language error responses.

## Highlights
- **Cache → DB → Provider pipeline** powered by Garnet/Redis, PostgreSQL, and prioritized providers (Google Books, Kakao Book, Aladin, MusicBrainz, Discogs, UpcItemDb+TMDb).
- **Provider resilience** using `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` (retry, circuit breaker, timeout) with structured Serilog logs.
- **Rate limiting & ProblemDetails** middleware with localized (KO) messages for predictable client errors.
- **280 automated tests** (unit, integration, contract) execute on every `dotnet test` and inside CI.
- **Container-ready** via multi-stage `Containerfile`, dev `podman-compose`, prod `docker-compose.prod.yml`, and a GHCR publishing pipeline (`.github/workflows/ci-cd.yml`).

## Tech stack
- .NET 10.0, ASP.NET Core Minimal APIs, EF Core 10
- PostgreSQL 16 (production), EF InMemory (dev/test), Garnet (Redis-compatible cache)
- Serilog, `Microsoft.Extensions.Http.Resilience`, StackExchange.Redis
- Swagger / OpenAPI, xUnit + Moq + FluentAssertions

## Architecture
```
CollectionServer/
├── src/
│   ├── CollectionServer.Api            # Minimal API host, middleware, DI
│   ├── CollectionServer.Core           # Entities, services, validators, interfaces
│   └── CollectionServer.Infrastructure # EF DbContext, repositories, external providers
├── tests/                              # Unit, integration, contract suites
├── docs/                               # API & deployment guides
├── specs/                              # Feature specifications & plans
├── podman-compose.yml                  # Dev container stack (InMemory DB path)
└── docker-compose.prod.yml             # Prod-like stack (Postgres + Garnet + Nginx)
```

## Provider coverage (2025-11-27)
| Domain | Provider | Status | Barcode support | Notes |
| --- | --- | --- | --- | --- |
| Books | Google Books | ✅ Production ready | ISBN-10/13 | Full metadata + cover art |
| Books | Kakao Book | ✅ Production ready | ISBN-10/13 | Korean catalog priority |
| Books/Music/DVD | Aladin API | ✅ Production ready | ISBN-10/13, UPC/EAN-13 | mallType-aware mapping for books, music albums, DVDs |
| Music | MusicBrainz | ✅ Production ready | UPC/EAN-13 | Includes track listing when available |
| Music | Discogs | ✅ Production ready | UPC/EAN-13 | Two-step search (barcode → release → detail) |
| Movies | UpcItemDb + TMDb bridge | ✅ Production ready | UPC/EAN-13 (non ISBN) | UPCitemdb resolves title → TMDb fetches cast/metadata |
| Movies | TMDb | ⚠️ Stub | UPC/EAN-13 (non ISBN) | Direct barcode search unsupported; falls back to title search in bridge |
| Movies | OMDb | ⚠️ Stub | UPC (12 digits) | UPC-only API; awaits external UPC→IMDb mapping |

Fallback order is computed dynamically via provider `Priority` in `ExternalApis` settings (lower number = higher priority). MediaService logs every decision, making it simple to trace which provider handled a request.

## Local development
### Requirements
- .NET SDK 10.0.100 (see `global.json`)
- PostgreSQL 16 (only needed if you run against a database)
- Optional: Podman/Docker for container workflows

### Quick start
```bash
git clone <repo-url>
cd CollectionServer

dotnet restore
# Configure API keys (examples below) then run:
dotnet run --project src/CollectionServer.Api
```
The default `Development` environment uses the EF InMemory provider and a no-op cache so you can run the API without Postgres or Garnet. Swagger UI is available at `https://localhost:5001/swagger` (`http://localhost:5000/swagger` for HTTP).

### Configure API keys (local user-secrets)
```bash
cd src/CollectionServer.Api
dotnet user-secrets init

dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey"   "<google-key>"
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey"     "<kakao-key>"
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey"     "<aladin-ttb-key>"
dotnet user-secrets set "ExternalApis:Discogs:ApiKey"       "<discogs-token>"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret"    "<discogs-secret>"
dotnet user-secrets set "ExternalApis:TMDb:ApiKey"          "<tmdb-key>"
dotnet user-secrets set "ExternalApis:OMDb:ApiKey"          "<omdb-key>"
```
Secrets are never stored in Git—use `.env` / `.env.prod` templates for container scenarios.

### Sample requests
```bash
# Book lookup (ISBN-13)
curl http://localhost:5000/items/9788966262281

# Health
curl http://localhost:5000/health
```
Rate limiting defaults to **100 req/min** (dev) and **200 req/min** (prod). Exceeding the limit returns HTTP 429 with `Retry-After` headers and localized ProblemDetails payloads.

## Container workflows
### Developer stack (podman-compose)
```
podman-compose up -d
```
This builds the API using the multi-stage `Containerfile`, starts Postgres/Garnet, and exposes the API on `http://localhost:5283`. Because the service runs in `Development`, it still uses InMemory EF; use this stack to validate container build parity and local integration.

### Production-like stack (docker-compose.prod)
```
cp .env.prod.example .env.prod   # populate DB + API key secrets
cp .env.example .env             # optional: reuse for Discogs etc.

docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```
The prod stack runs with `ASPNETCORE_ENVIRONMENT=Production`, enabling PostgreSQL, Garnet caching, and serving through Nginx. Images are pulled from `ghcr.io/<your-repo>:latest`, which CI publishes on every push.

## Testing
All suites run via:
```bash
dotnet test
```
As of 2025-11-27 this executes **280 tests** (provider/unit suites, integration tests with mocked HTTP clients, and contract tests). Targeted runs:
```bash
# Provider-only unit tests
dotnet test --filter FullyQualifiedName~ProviderTests

# Integration tests
dotnet test tests/CollectionServer.IntegrationTests
```

## Documentation & specs
- `docs/api-guide.md` – Request/response examples, error catalog, rate limiting details.
- `docs/deployment.md` – System requirements, Kubernetes/systemd samples, backup/restore playbooks.
- `specs/001-isbn-book-api/*.md` – Feature spec, plans, tasks.

## Deployment & monitoring status
- `.github/workflows/ci-cd.yml` builds/tests on .NET 10 and pushes GHCR images with `latest` + commit SHA tags.
- `docker-compose.prod.yml` + `nginx/` provide a ready-to-run stack (Nginx → API → Postgres → Garnet).
- Structured Serilog JSON logs are emitted to console and `logs/collectionserver-*.log` for ingestion.

## Contributing
1. Fork and create a feature branch.
2. Run `dotnet test` (all suites).
3. Submit a PR after ensuring documentation is up to date.

Issues and feature requests are welcome through GitHub Issues.
