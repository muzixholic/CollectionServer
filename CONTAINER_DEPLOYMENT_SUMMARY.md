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

## TLS 구성 절차
1. **도메인 및 DNS**: `example.com` 같은 실제 도메인을 서버 공인 IP로 A/CNAME 매핑합니다.
2. **Certbot 준비**:
   ```bash
   docker-compose run --rm nginx sh -c "mkdir -p /var/www/certbot"
   docker-compose run --rm certbot certonly --webroot \
     -w /var/www/certbot \
     -d example.com -d www.example.com \
     --email admin@example.com --agree-tos --no-eff-email
   ```
3. **Nginx 설정**: `nginx/conf.d/default.conf`의 `server_name`, `ssl_certificate`, `ssl_certificate_key` 경로를 실제 도메인으로 교체합니다.
4. **재기동**: `docker-compose restart nginx` 후 `https://example.com/health`로 TLS/프록시 상태를 확인합니다.
5. **자동 갱신**: `certbot renew`를 cron 또는 GitHub Actions 템플릿(`ops/certbot-renew.workflow.yml` → `.github/workflows/certbot-renew.yml`)으로 주기 실행하고, 인증서 갱신 후 `nginx`를 재로드합니다.

## 모니터링 스택
- 위치: `ops/monitoring/docker-compose.monitoring.yml`
- 구성: Prometheus(9090) + Alertmanager(9093) + Grafana(3000) + Loki/Promtail(3100/9080) + Tempo(4317/3200)
- 실행 전제: 메인 서비스와 동일한 `collectionserver-network` 외부 네트워크, API 로그 디렉터리(`./logs`) 존재
- 실행 명령: `docker compose -f ops/monitoring/docker-compose.monitoring.yml up -d`
- Alertmanager 수신자, Promtail 로그 경로, Grafana Admin 비밀번호 등은 각 설정 파일에서 수정 가능

## 다음 단계
- [ ] 실 서버 또는 Kubernetes 환경에 `docker-compose.prod.yml`을 적용하고 헬스 체크를 자동화합니다.
- [ ] 위 TLS 절차를 적용해 HTTPS를 기본으로 운영합니다.
- [ ] 모니터링 스택을 기동하고 Grafana 대시보드/알림 규칙을 구성합니다.
- [ ] 배포 후 `curl https://<domain>/health`와 같은 헬스 점검 스크립트를 추가합니다.
