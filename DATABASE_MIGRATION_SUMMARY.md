# Database & Migration Status (2025-11-27)

## TL;DR
- 🟢 **Production**: targets PostgreSQL 16 with retry-enabled Npgsql 10 driver + Garnet cache (`AddDatabaseServices`).
- 🟡 **Development/Test**: intentionally use EF InMemory + Fake cache to keep inner-loop fast and avoid PostgreSQL dependencies.
- 🔜 **Next step**: create and commit the initial EF Core migration once a managed Postgres instance is provisioned (or run `docker-compose.prod.yml`).

## 현재 동작 방식
| 환경 | DB | Cache | 구성 위치 |
| --- | --- | --- | --- |
| `Development` / `Testing` | EF InMemory (`UseInMemoryDatabase("CollectionServerDev")`) | Fake cache (tests) / optional no-op | `Program.cs` (lines 28-44) |
| `Production` (또는 커스텀) | PostgreSQL 16 (`UseNpgsql`) | Garnet (`StackExchange.Redis` via `GarnetCacheService`) | `AddDatabaseServices` + connection strings |

`docker-compose.prod.yml` 와 GitHub Actions는 `ASPNETCORE_ENVIRONMENT=Production` 으로 실행하므로 Postgres + Garnet 경로가 활성화됩니다.

## 왜 InMemory를 유지하나요?
1. **Developer velocity** – EF InMemory + Seedless 데이터로 API를 즉시 실행 가능.
2. **Test isolation** – Integration/Contract 테스트가 InMemory DB를 각 클래스별로 분리하여 빠르게 수행.
3. **Npgsql 10 안정화 완료** – 현재는 호환성 문제가 없지만, 실제 Postgres 인스턴스가 준비될 때까지 Schema drift를 피하기 위해 Migration을 보류 중.

## Postgres로 전환하려면
1. 로컬 또는 클라우드 Postgres 16 인스턴스를 준비합니다.
2. `ConnectionStrings:DefaultConnection` 값을 `.env.prod` 또는 User Secrets에 설정합니다.
3. 개발 중 Postgres를 사용하고 싶다면 `ASPNETCORE_ENVIRONMENT=Production` 으로 기동하거나, Program.cs에 임시 플래그를 추가합니다.
4. (선택) `docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d` 로 prod-like 환경을 띄웁니다.

## Migration 계획
1. **Baseline Migration 작성**
   ```bash
   dotnet ef migrations add InitialCreate \
     --project src/CollectionServer.Infrastructure \
     --startup-project src/CollectionServer.Api
   ```
2. **DB 적용**
   ```bash
   dotnet ef database update \
     --project src/CollectionServer.Infrastructure \
     --startup-project src/CollectionServer.Api
   ```
3. **CI 통합** – GitHub Actions에 `dotnet ef database update` 단계를 추가하여 schema drift 방지.
4. **Prod 배포** – `docker-compose.prod.yml` 에 마이그레이션 스텝 (예: `api` 컨테이너 entrypoint) 을 추가하거나, 별도의 migration job 실행.

## 남은 과제
- [ ] `InitialCreate` migration 커밋
- [ ] Integration 테스트를 real Postgres 컨테이너로 실행하는 CI job 추가
- [ ] `appsettings.Development.json` 에 선택적 Postgres 스위치 문서화
- [ ] Monitoring (pg_stat_statements, pgBouncer 등) 가이드 `docs/deployment.md` 에 추가
