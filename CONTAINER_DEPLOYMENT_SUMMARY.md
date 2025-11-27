# 컨테이너 & 배포 요약 (2025-11-27)

## 현재 상태
- ✅ **Dev stack (`podman-compose.yml`)**: 멀티스테이지 이미지를 빌드하고 Postgres/Garnet 컨테이너를 함께 띄웁니다. API는 `Development` 환경으로 동작하므로 InMemory EF를 계속 사용하며, 컨테이너 빌드/런타임 검증용으로 활용합니다.
- ✅ **Prod stack (`docker-compose.prod.yml`)**: Nginx + API + PostgreSQL + Garnet을 하나의 Compose 네트워크로 구성합니다. API는 `Production` 환경으로 실행되며 DB/Cache 경로를 모두 활성화합니다.
- ✅ **CI/CD (`.github/workflows/ci-cd.yml`)**: main 브랜치 푸시 시 `dotnet test` → 컨테이너 빌드 → GHCR(`ghcr.io/<repo>:latest` & `:<sha>`) 푸시 → compose 스택에서 pull.
- ⏳ **실 서버 배포**: 서버/도메인/인증서 준비 후 `docker-compose.prod.yml` + `nginx/conf.d/default.conf` 로 즉시 런칭 가능.

## Dev stack 실행
```bash
podman-compose up -d
podman-compose logs -f api
```
- API: http://localhost:5283
- Postgres: 포트 5432 (health check 포함)
- Garnet: 포트 6379 (Redis 클라이언트 호환)
- ※ `ASPNETCORE_ENVIRONMENT=Development` 이므로 실제 Postgres 대신 InMemory DB 사용 (ConnectionStrings 는 향후 전환 대비용)

## Prod stack 실행
```bash
cp .env.prod.example .env.prod
cp .env.example .env    # 선택 (Discogs 등)

docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```
### 구성 요소
| 서비스 | 설명 | 포트 | 비고 |
| --- | --- | --- | --- |
| nginx | Reverse Proxy + SSL/TLS | 80/443 | `nginx/conf.d` 템플릿 제공, Certbot 볼륨 포함 |
| postgres | PostgreSQL 16 | 내부 | 헬스체크 + Volume (`postgres_data`) |
| garnet | Redis 호환 캐시 | 내부 | `ConnectionStrings__CacheConnection` 로 주입 |
| api | CollectionServer | 8080 (→ nginx) | GHCR 이미지 사용, `.env.prod` 값 주입 |

## CI/CD 파이프라인
1. `dotnet restore && dotnet test` (SDK 10.0.100)
2. `podman build` 를 이용한 멀티스테이지 이미지 빌드
3. GHCR 푸시: `ghcr.io/${{ github.repository }}:{latest, sha}`
4. Release 노트에 이미지 digest 기록 (선택)

## 다음 단계
- [ ] 실 서버(또는 K8s) 환경 준비 후 `docker-compose.prod.yml` 적용
- [ ] HTTPS 인증서(Certbot) 발급 및 `nginx/conf.d/default.conf` 의 도메인 업데이트
- [ ] Post-deploy 건강 검진 스크립트 (`curl https://<domain>/health`) 자동화
- [ ] Grafana/Prometheus 또는 클라우드 모니터링 연동 (garnet/postgres/api 지표)
