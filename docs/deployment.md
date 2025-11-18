# 배포 가이드

CollectionServer를 프로덕션 환경에 배포하는 방법을 설명합니다.

## 목차
- [시스템 요구사항](#시스템-요구사항)
- [환경 변수](#환경-변수)
- [데이터베이스 설정](#데이터베이스-설정)
- [Podman/Docker 배포](#podmandocker-배포)
- [수동 배포](#수동-배포)
- [모니터링](#모니터링)
- [백업 및 복구](#백업-및-복구)

## 시스템 요구사항

### 최소 사양
- **CPU**: 2 코어
- **메모리**: 2GB RAM
- **디스크**: 20GB
- **OS**: Linux (Ubuntu 22.04 LTS 권장)

### 권장 사양
- **CPU**: 4 코어
- **메모리**: 4GB RAM
- **디스크**: 50GB SSD
- **OS**: Linux (Ubuntu 22.04 LTS)

### 필수 소프트웨어
- .NET 10.0 Runtime (또는 Podman/Docker)
- PostgreSQL 16+
- Podman 4.0+ 또는 Docker 24.0+

## 환경 변수

프로덕션 환경에서는 환경 변수를 통해 설정을 주입합니다.

### 필수 환경 변수

```bash
# 데이터베이스 연결
export DATABASE_CONNECTION_STRING="Host=postgres-server;Database=collectionserver;Username=dbuser;Password=strong-password;Maximum Pool Size=100;Connection Idle Lifetime=300"

# 외부 API 키
export GOOGLE_BOOKS_API_KEY="your-google-api-key"
export KAKAO_API_KEY="your-kakao-api-key"
export TMDB_API_KEY="your-tmdb-api-key"
export OMDB_API_KEY="your-omdb-api-key"
export DISCOGS_API_KEY="your-discogs-key"
export DISCOGS_API_SECRET="your-discogs-secret"
```

### 선택적 환경 변수

```bash
# ASP.NET Core 환경
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://+:5000"

# 로깅 레벨
export Logging__LogLevel__Default="Warning"
export Logging__LogLevel__Microsoft="Warning"
```

## 데이터베이스 설정

### PostgreSQL 설치 (Ubuntu)

```bash
# PostgreSQL 16 설치
sudo apt update
sudo apt install -y postgresql-16 postgresql-contrib-16

# PostgreSQL 시작
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### 데이터베이스 및 사용자 생성

```bash
sudo -u postgres psql <<EOF
CREATE DATABASE collectionserver;
CREATE USER dbuser WITH ENCRYPTED PASSWORD 'strong-password';
GRANT ALL PRIVILEGES ON DATABASE collectionserver TO dbuser;
ALTER DATABASE collectionserver OWNER TO dbuser;
\c collectionserver
GRANT ALL ON SCHEMA public TO dbuser;
EOF
```

### 성능 튜닝 (postgresql.conf)

```ini
# 연결 설정
max_connections = 200
shared_buffers = 1GB
effective_cache_size = 3GB
maintenance_work_mem = 256MB
work_mem = 16MB

# WAL 설정
wal_buffers = 16MB
checkpoint_completion_target = 0.9
```

### 마이그레이션 적용

```bash
# .NET SDK가 설치된 환경에서
dotnet ef database update \
  --project src/CollectionServer.Infrastructure \
  --startup-project src/CollectionServer.Api \
  --connection "$DATABASE_CONNECTION_STRING"
```

## Podman/Docker 배포

### 1. 이미지 빌드

```bash
# Podman
podman build -t collectionserver:latest -f Containerfile .

# Docker
docker build -t collectionserver:latest -f Containerfile .
```

### 2. Podman Compose로 배포

`podman-compose.yml` 파일 사용:

```bash
# 시작
podman-compose up -d

# 로그 확인
podman-compose logs -f api

# 중지
podman-compose down
```

### 3. 개별 컨테이너 실행

```bash
# PostgreSQL 컨테이너
podman run -d \
  --name postgres \
  -e POSTGRES_DB=collectionserver \
  -e POSTGRES_USER=dbuser \
  -e POSTGRES_PASSWORD=strong-password \
  -v pgdata:/var/lib/postgresql/data \
  -p 5432:5432 \
  docker.io/library/postgres:16

# API 컨테이너
podman run -d \
  --name collectionserver \
  -e DATABASE_CONNECTION_STRING="Host=postgres;Database=collectionserver;Username=dbuser;Password=strong-password" \
  -e GOOGLE_BOOKS_API_KEY="$GOOGLE_BOOKS_API_KEY" \
  -e KAKAO_API_KEY="$KAKAO_API_KEY" \
  -p 5000:5000 \
  --link postgres:postgres \
  collectionserver:latest
```

### 4. Kubernetes 배포 (선택사항)

Deployment 예시:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: collectionserver
spec:
  replicas: 3
  selector:
    matchLabels:
      app: collectionserver
  template:
    metadata:
      labels:
        app: collectionserver
    spec:
      containers:
      - name: api
        image: collectionserver:latest
        ports:
        - containerPort: 5000
        env:
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: GOOGLE_BOOKS_API_KEY
          valueFrom:
            secretKeyRef:
              name: api-keys
              key: google-books
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
```

## 수동 배포

### 1. 애플리케이션 빌드

```bash
dotnet publish src/CollectionServer.Api/CollectionServer.Api.csproj \
  -c Release \
  -o /opt/collectionserver \
  --self-contained false
```

### 2. Systemd 서비스 생성

`/etc/systemd/system/collectionserver.service`:

```ini
[Unit]
Description=CollectionServer API
After=network.target postgresql.service

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/collectionserver
ExecStart=/usr/bin/dotnet /opt/collectionserver/CollectionServer.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=collectionserver
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000
EnvironmentFile=/etc/collectionserver/env

[Install]
WantedBy=multi-user.target
```

### 3. 환경 변수 파일

`/etc/collectionserver/env`:

```bash
DATABASE_CONNECTION_STRING=Host=localhost;Database=collectionserver;Username=dbuser;Password=strong-password
GOOGLE_BOOKS_API_KEY=your-key
KAKAO_API_KEY=your-key
TMDB_API_KEY=your-key
```

권한 설정:
```bash
sudo chmod 600 /etc/collectionserver/env
sudo chown www-data:www-data /etc/collectionserver/env
```

### 4. 서비스 시작

```bash
sudo systemctl daemon-reload
sudo systemctl enable collectionserver
sudo systemctl start collectionserver
sudo systemctl status collectionserver
```

### 5. Nginx 리버스 프록시 (선택사항)

`/etc/nginx/sites-available/collectionserver`:

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Rate limiting
        limit_req zone=api burst=20 nodelay;
    }
}
```

Nginx Rate Limiting 설정:

```nginx
http {
    limit_req_zone $binary_remote_addr zone=api:10m rate=100r/m;
    # ...
}
```

## 모니터링

### 헬스 체크

```bash
curl http://localhost:5000/health
```

### 로그 확인

```bash
# Systemd 서비스
sudo journalctl -u collectionserver -f

# Podman
podman logs -f collectionserver

# 로그 파일
tail -f /opt/collectionserver/logs/collectionserver-*.log
```

### 메트릭 수집 (Prometheus 예시)

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'collectionserver'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: /metrics
```

## 백업 및 복구

### 데이터베이스 백업

```bash
# 전체 백업
pg_dump -h localhost -U dbuser collectionserver | gzip > backup_$(date +%Y%m%d_%H%M%S).sql.gz

# 스케줄 백업 (cron)
0 2 * * * pg_dump -h localhost -U dbuser collectionserver | gzip > /backups/collectionserver_$(date +\%Y\%m\%d).sql.gz
```

### 데이터베이스 복구

```bash
# 복구
gunzip -c backup_20250118.sql.gz | psql -h localhost -U dbuser collectionserver
```

### 컨테이너 볼륨 백업

```bash
# PostgreSQL 볼륨 백업
podman run --rm \
  -v pgdata:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/pgdata_backup.tar.gz -C /data .
```

## 보안 체크리스트

- [ ] PostgreSQL 원격 접근 제한
- [ ] 강력한 데이터베이스 비밀번호 사용
- [ ] 환경 변수 파일 권한 설정 (600)
- [ ] HTTPS 사용 (Nginx + Let's Encrypt)
- [ ] API 키 환경 변수로 관리
- [ ] Rate Limiting 활성화
- [ ] 정기적인 보안 패치
- [ ] 로그 모니터링

## 트러블슈팅

### 데이터베이스 연결 실패

```bash
# PostgreSQL 상태 확인
sudo systemctl status postgresql

# 연결 테스트
psql -h localhost -U dbuser -d collectionserver
```

### 메모리 부족

`appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Maximum Pool Size=50;..."
  }
}
```

### 높은 응답 시간

- 데이터베이스 인덱스 확인
- 연결 풀 크기 조정
- 쿼리 최적화 (EXPLAIN ANALYZE)

## 업데이트 및 롤백

### 무중단 업데이트 (Kubernetes)

```bash
kubectl set image deployment/collectionserver api=collectionserver:v1.1.0
kubectl rollout status deployment/collectionserver
```

### 롤백

```bash
kubectl rollout undo deployment/collectionserver
```

### Systemd 서비스 업데이트

```bash
# 새 버전 배포
dotnet publish -c Release -o /opt/collectionserver-new

# 서비스 중지
sudo systemctl stop collectionserver

# 파일 교체
sudo mv /opt/collectionserver /opt/collectionserver-backup
sudo mv /opt/collectionserver-new /opt/collectionserver

# 서비스 시작
sudo systemctl start collectionserver
```

## 지원

문제가 발생하면 다음을 확인하세요:
1. 로그 파일 (`journalctl -u collectionserver`)
2. 데이터베이스 연결
3. 외부 API 키 설정
4. 네트워크 방화벽 규칙

GitHub Issues: [링크]
