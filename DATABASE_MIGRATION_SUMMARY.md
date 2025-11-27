# 데이터베이스 및 마이그레이션 현황 (2025-11-27)

## 요약
- 🟢 **운영/배포**: PostgreSQL 16 + Npgsql 10 + Garnet 캐시 경로(`AddDatabaseServices`)가 준비되어 있습니다.
- 🟡 **개발/테스트**: 빠른 피드백을 위해 EF InMemory + Fake 캐시를 사용합니다.
- 🔜 **다음 단계**: 관리형 PostgreSQL이 확보되면 초기 EF Core 마이그레이션을 생성/커밋하고 CI/CD에 자동 적용 단계를 추가합니다.

## 환경별 동작
| 환경 | DB | 캐시 | 구성 위치 |
| --- | --- | --- | --- |
| Development / Testing | EF InMemory (`UseInMemoryDatabase("CollectionServerDev")`) | FakeCacheService (테스트) / No-Op | `Program.cs` (28~44행) |
| Production | PostgreSQL 16 (`UseNpgsql`) | Garnet (`GarnetCacheService`) | `ServiceCollectionExtensions.AddDatabaseServices` |

`docker-compose.prod.yml`과 GitHub Actions 파이프라인은 항상 `ASPNETCORE_ENVIRONMENT=Production`으로 실행되므로 Postgres + Garnet 경로가 활성화됩니다. 또한 Program.cs에서 운영 환경일 경우 `Database.MigrateAsync()`를 호출하므로 API 컨테이너가 기동될 때마다 최신 마이그레이션이 자동 적용됩니다. CI 환경에서 PostgreSQL을 띄워 마이그레이션을 검증하려면 `ops/ci-with-postgres.workflow.yml` 템플릿을 `.github/workflows`로 복사하여 사용하세요.

## 왜 InMemory를 유지하나요?
1. **개발 속도** – 로컬에서 Postgres 없이 즉시 실행 가능.
2. **테스트 격리** – 통합/계약 테스트가 클래스별 고유 DB 이름을 사용하여 빠르게 수행됩니다.
3. **전환 대비** – Npgsql 10이 안정화된 현재에도, 실제 운영 DB가 준비될 때까지 스키마 드리프트를 피하기 위해 마이그레이션을 보류하고 있습니다.

## PostgreSQL로 전환하려면
1. 로컬 또는 클라우드 PostgreSQL 16 인스턴스를 준비합니다.
2. `ConnectionStrings:DefaultConnection`을 `.env.prod` 또는 User Secrets에 실제 값으로 설정합니다.
3. 개발 환경에서도 Postgres를 사용하고 싶다면 `ASPNETCORE_ENVIRONMENT=Production`으로 실행하거나, Program.cs에 임시 스위치를 추가합니다.
4. `docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d` 명령으로 운영 유사 환경을 띄워 검증합니다.

## 마이그레이션 계획
1. **Baseline 마이그레이션 생성** – `InitialCreate` 마이그레이션이 이미 `src/CollectionServer.Infrastructure/Data/Migrations` 경로에 추가되었습니다.
   ```bash
   dotnet ef migrations add InitialCreate \
     --project src/CollectionServer.Infrastructure \
     --startup-project src/CollectionServer.Api \
     --output-dir Data/Migrations
   ```
2. **DB 적용**
   ```bash
   dotnet ef database update \
     --project src/CollectionServer.Infrastructure \
     --startup-project src/CollectionServer.Api
   ```
3. **CI 통합** – GitHub Actions에 `dotnet ef database update` 단계를 추가하여 스키마 동기화를 자동화합니다.
4. **운영 배포** – `docker-compose.prod.yml`의 API 서비스 시작 전에 마이그레이션을 실행하거나 별도 Job을 추가합니다.

## 남은 TODO
- [ ] `InitialCreate` 마이그레이션 커밋
- [ ] 실 Postgres 컨테이너를 사용하는 통합 테스트 Job 추가
- [ ] `appsettings.Development.json`에 Postgres 전환 플래그 문서화
- [ ] `docs/deployment.md`에 Postgres 모니터링/백업 가이드 추가
