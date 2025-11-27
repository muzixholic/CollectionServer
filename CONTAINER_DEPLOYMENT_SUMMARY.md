# 컨테이너 및 배포 요약 (2025-11-27)

## 현재 상태
- ✅ **개발 스택(`podman-compose.yml`)**: 멀티 스테이지 이미지를 직접 빌드하고 Postgres/Garnet 컨테이너를 함께 실행합니다. API는 `Development` 환경으로 동작하여 InMemory DB를 유지한 채 컨테이너 호환성만 검증할 수 있습니다.
- ✅ **운영 스택(`docker-compose.prod.yml`)**: Nginx + API + PostgreSQL + Garnet을 하나의 네트워크로 구성하며 `ASPNETCORE_ENVIRONMENT=Production`으로 실행됩니다.
- ✅ **CI/CD (`.github/workflows/ci-cd.yml`)**: main 브랜치 푸시 시 `dotnet test` → 컨테이너 빌드 → GHCR(`ghcr.io/<repo>:latest`, `<sha>`) 푸시가 자동으로 수행됩니다.
- ⏳ **실 서버 배포**: 서버/도메인/TLS 인증서가 준비되면 `docker-compose.prod.yml`과 `nginx/conf.d/default.conf`만으로 즉시 배포할 수 있습니다.

## 개발 스택 실행
```bash
podman-compose up -d
podman-compose logs -f api
```
- API: http://localhost:5283
- Postgres: 5432 (health check 포함)
- Garnet: 6379 (Redis 호환)
- `env_file: .env`를 사용해 API 키를 주입합니다.

## 운영 스택 실행
```bash
cp .env.prod.example .env.prod   # DB/비밀 값 작성
cp .env.example .env            # 선택: Discogs 등 추가 키

docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```
| 서비스 | 설명 | 포트 | 비고 |
| --- | --- | --- | --- |
| nginx | Reverse Proxy + TLS | 80/443 | `nginx/conf.d` 템플릿 + Certbot 볼륨 |
| postgres | PostgreSQL 16 | 내부 | `postgres_data` 볼륨 + 헬스체크 |
| garnet | Redis 호환 캐시 | 내부 | `ConnectionStrings__CacheConnection`을 통해 API에 주입 |
| api | CollectionServer | 8080 (→ nginx) | GHCR 이미지 사용, `.env.prod` 값 주입 |

## CI/CD 파이프라인
1. `dotnet restore && dotnet test`
2. 멀티 스테이지 컨테이너 빌드 (`podman build` / `docker build`)
3. GHCR 푸시(`latest`, `commit SHA` 태그)
4. (선택) 릴리스 노트에 이미지 Digest 기록

## 다음 단계
- [ ] 실 서버 또는 Kubernetes 환경에 `docker-compose.prod.yml`을 적용하고 헬스 체크를 자동화합니다.
- [ ] Certbot 또는 클라우드 인증서를 이용해 TLS를 구성하고 `nginx/conf.d/default.conf`에 도메인을 반영합니다.
- [ ] 배포 후 `curl https://<domain>/health`와 같은 헬스 점검 스크립트를 추가합니다.
- [ ] Grafana/Prometheus 혹은 클라우드 모니터링에 Postgres·Garnet·API 지표를 연동합니다.
