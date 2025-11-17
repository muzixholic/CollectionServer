# Quickstart Guide: 미디어 정보 API 서버

**Feature**: 001-isbn-book-api  
**Date**: 2025-11-16  
**Tech Stack**: .NET 10.0, C# 13, ASP.NET Core 10.0, PostgreSQL 16+

## 목적

미디어 정보 API 서버의 개발 환경을 신속하게 설정하고 첫 API 요청을 실행하는 방법을 안내합니다.

---

## 사전 요구사항

### 필수 소프트웨어

| 소프트웨어 | 버전 | 설치 확인 명령 | 다운로드 링크 |
|----------|------|---------------|-------------|
| .NET SDK | 10.0.100+ | `dotnet --version` | [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 16+ | `psql --version` | [https://www.postgresql.org/download/](https://www.postgresql.org/download/) |
| Git | 최신 | `git --version` | [https://git-scm.com/downloads](https://git-scm.com/downloads) |
| Podman (선택) | 최신 | `podman --version` | [https://podman.io/getting-started/installation](https://podman.io/getting-started/installation) |

### 설치 확인

```bash
# .NET 10 SDK 확인
dotnet --version
# 출력 예: 10.0.100

# .NET 런타임 목록
dotnet --list-runtimes
# 출력에 "Microsoft.NETCore.App 10.0.0" 포함 확인

# PostgreSQL 확인
psql --version
# 출력 예: psql (PostgreSQL) 16.1
```

---

## 1. 프로젝트 설정

### 1.1 리포지토리 클론

```bash
git clone https://github.com/your-org/CollectionServer.git
cd CollectionServer
git checkout 001-isbn-book-api
```

### 1.2 global.json 생성

프로젝트 루트에 `global.json` 파일 생성:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

### 1.3 솔루션 및 프로젝트 생성

```bash
# 솔루션 생성
dotnet new sln -n CollectionServer

# API 프로젝트 (ASP.NET Core 10.0 Web API)
dotnet new webapi -n CollectionServer.Api -o src/CollectionServer.Api --framework net10.0

# Core 프로젝트 (클래스 라이브러리)
dotnet new classlib -n CollectionServer.Core -o src/CollectionServer.Core --framework net10.0

# Infrastructure 프로젝트 (클래스 라이브러리)
dotnet new classlib -n CollectionServer.Infrastructure -o src/CollectionServer.Infrastructure --framework net10.0

# 테스트 프로젝트
dotnet new xunit -n CollectionServer.UnitTests -o tests/CollectionServer.UnitTests --framework net10.0
dotnet new xunit -n CollectionServer.IntegrationTests -o tests/CollectionServer.IntegrationTests --framework net10.0

# 솔루션에 프로젝트 추가
dotnet sln add src/CollectionServer.Api/CollectionServer.Api.csproj
dotnet sln add src/CollectionServer.Core/CollectionServer.Core.csproj
dotnet sln add src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj
dotnet sln add tests/CollectionServer.UnitTests/CollectionServer.UnitTests.csproj
dotnet sln add tests/CollectionServer.IntegrationTests/CollectionServer.IntegrationTests.csproj

# 프로젝트 참조 추가
dotnet add src/CollectionServer.Api reference src/CollectionServer.Core
dotnet add src/CollectionServer.Api reference src/CollectionServer.Infrastructure
dotnet add src/CollectionServer.Infrastructure reference src/CollectionServer.Core
dotnet add tests/CollectionServer.UnitTests reference src/CollectionServer.Core
dotnet add tests/CollectionServer.IntegrationTests reference src/CollectionServer.Api
```

### 1.4 프로젝트 파일 설정 (.csproj)

각 프로젝트 파일에서 `<LangVersion>` 추가:

**src/CollectionServer.Api/CollectionServer.Api.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**src/CollectionServer.Core/CollectionServer.Core.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## 2. NuGet 패키지 설치

### 2.1 API 프로젝트 패키지

```bash
cd src/CollectionServer.Api

# OpenAPI/Swagger 지원
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.0
dotnet add package Swashbuckle.AspNetCore --version 7.0.0

# 로깅
dotnet add package Serilog.AspNetCore --version 10.0.0
dotnet add package Serilog.Formatting.Compact --version 3.0.0

cd ../..
```

### 2.2 Infrastructure 프로젝트 패키지

```bash
cd src/CollectionServer.Infrastructure

# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.0

cd ../..
```

### 2.3 테스트 프로젝트 패키지

```bash
cd tests/CollectionServer.UnitTests

dotnet add package xunit --version 2.9.0
dotnet add package xunit.runner.visualstudio --version 2.8.2
dotnet add package Moq --version 4.20.0
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.11.0

cd ../..
```

### 2.4 패키지 복원 확인

```bash
dotnet restore
dotnet build
```

---

## 3. PostgreSQL 데이터베이스 설정

### 옵션 A: 로컬 PostgreSQL 설치

```bash
# PostgreSQL 데이터베이스 생성
psql -U postgres
CREATE DATABASE collectiondb;
CREATE USER collectionuser WITH ENCRYPTED PASSWORD 'YourSecurePassword123!';
GRANT ALL PRIVILEGES ON DATABASE collectiondb TO collectionuser;
\q
```

### 옵션 B: Podman 컨테이너 (권장)

**podman-compose.yml** 생성 (프로젝트 루트):

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: collectiondb
    environment:
      POSTGRES_DB: collectiondb
      POSTGRES_USER: collectionuser
      POSTGRES_PASSWORD: YourSecurePassword123!
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  postgres_data:
```

```bash
# podman-compose로 PostgreSQL 시작 (podman-compose 설치 필요)
podman-compose up -d

# 또는 Podman pod 사용 (podman-compose 없이)
podman pod create --name collectionserver -p 5432:5432
podman run -d --pod collectionserver \
  --name collectiondb \
  -e POSTGRES_DB=collectiondb \
  -e POSTGRES_USER=collectionuser \
  -e POSTGRES_PASSWORD=YourSecurePassword123! \
  -v postgres_data:/var/lib/postgresql/data \
  postgres:16

# 상태 확인
podman ps

# 로그 확인
podman logs collectiondb
```

### 연결 문자열 설정

**src/CollectionServer.Api/appsettings.Development.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=collectiondb;Username=collectionuser;Password=YourSecurePassword123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 4. Entity Framework Core 마이그레이션

### 4.1 EF Core 도구 설치 (글로벌)

```bash
dotnet tool install --global dotnet-ef --version 10.0.0

# 설치 확인
dotnet ef --version
# 출력 예: Entity Framework Core .NET Command-line Tools 10.0.0
```

### 4.2 초기 마이그레이션 생성

```bash
cd src/CollectionServer.Infrastructure

# 초기 마이그레이션 생성
dotnet ef migrations add InitialCreate --startup-project ../CollectionServer.Api

# 마이그레이션 SQL 미리보기 (선택)
dotnet ef migrations script --startup-project ../CollectionServer.Api

cd ../..
```

### 4.3 데이터베이스 업데이트

```bash
cd src/CollectionServer.Infrastructure

# 데이터베이스에 마이그레이션 적용
dotnet ef database update --startup-project ../CollectionServer.Api

cd ../..
```

### 4.4 마이그레이션 확인

```bash
# PostgreSQL에 연결하여 테이블 확인
psql -U collectionuser -d collectiondb -h localhost

# 테이블 목록 조회
\dt

# 예상 출력:
#  Schema |       Name        | Type  |     Owner      
# --------+-------------------+-------+----------------
#  public | __EFMigrationsHistory | table | collectionuser
#  public | media_items       | table | collectionuser
#  public | books             | table | collectionuser
#  public | movies            | table | collectionuser
#  public | music_albums      | table | collectionuser

\q
```

---

## 5. 애플리케이션 실행

### 5.1 개발 서버 시작

```bash
cd src/CollectionServer.Api

# HTTPS 개발 인증서 신뢰 (최초 1회)
dotnet dev-certs https --trust

# 애플리케이션 실행
dotnet run

# 출력 예:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:8080
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

### 5.2 Swagger UI 접속

브라우저에서 열기:
- **Swagger UI**: http://localhost:8080/swagger
- **OpenAPI JSON**: http://localhost:8080/swagger/v1/swagger.json

---

## 6. 첫 API 요청 테스트

### 6.1 헬스 체크

```bash
curl http://localhost:8080/health
```

**예상 응답**:
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2025-11-16T14:30:00Z"
}
```

### 6.2 미디어 조회 (도서 예제)

```bash
# Effective Java 도서 조회
curl http://localhost:8080/items/9780134685991
```

**예상 응답** (첫 요청 - 외부 API 호출):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "barcode": "9780134685991",
  "mediaType": "Book",
  "title": "Effective Java",
  "description": "The definitive guide to Java programming",
  "imageUrl": "https://books.google.com/books/content?id=...",
  "source": "GoogleBooks",
  "createdAt": "2025-11-16T14:30:00Z",
  "updatedAt": "2025-11-16T14:30:00Z",
  "isbn13": "9780134685991",
  "authors": ["Joshua Bloch"],
  "publisher": "Addison-Wesley",
  "publishDate": "2018-01-06",
  "pageCount": 416,
  "genre": "Computer Science"
}
```

**재요청** (Database-First 캐싱):
```bash
# 동일 요청 - 데이터베이스에서 즉시 반환 (<50ms)
curl http://localhost:8080/items/9780134685991
```

### 6.3 오류 케이스 테스트

**잘못된 바코드 형식**:
```bash
curl http://localhost:8080/items/12345
```

**응답** (400 Bad Request):
```json
{
  "error": {
    "code": "INVALID_BARCODE_FORMAT",
    "message": "The provided barcode format is invalid. Expected ISBN-10, ISBN-13, UPC, or EAN-13.",
    "details": {
      "provided": "12345",
      "expectedFormats": [
        "ISBN-10 (10 digits)",
        "ISBN-13 (13 digits)",
        "UPC (12 digits)",
        "EAN-13 (13 digits)"
      ]
    }
  }
}
```

**존재하지 않는 미디어**:
```bash
curl http://localhost:8080/items/9999999999999
```

**응답** (404 Not Found):
```json
{
  "error": {
    "code": "MEDIA_NOT_FOUND",
    "message": "No media found for the provided barcode after checking all sources.",
    "details": {
      "barcode": "9999999999999",
      "sourcesChecked": ["GoogleBooks", "KakaoBooks", "Aladin"]
    }
  }
}
```

---

## 7. 외부 API 키 설정 (필수)

### 7.1 API 키 발급

| API | 무료 할당량 | 발급 링크 |
|-----|-----------|----------|
| Google Books API | 1000 req/day | [https://console.cloud.google.com/apis/library/books.googleapis.com](https://console.cloud.google.com/apis/library/books.googleapis.com) |
| Kakao Book Search | 300 req/day | [https://developers.kakao.com/](https://developers.kakao.com/) |
| Aladin API | 무제한 (개인용) | [https://blog.aladin.co.kr/openapi/category/1](https://blog.aladin.co.kr/openapi/category/1) |
| TMDb (Movie DB) | 충분 | [https://www.themoviedb.org/settings/api](https://www.themoviedb.org/settings/api) |
| OMDb API | 1000 req/day | [http://www.omdbapi.com/apikey.aspx](http://www.omdbapi.com/apikey.aspx) |
| MusicBrainz | 1 req/sec | 키 불필요 (User-Agent 헤더 필수) |
| Discogs API | 60 req/min | [https://www.discogs.com/settings/developers](https://www.discogs.com/settings/developers) |

### 7.2 User Secrets 설정 (권장)

```bash
cd src/CollectionServer.Api

# User Secrets 초기화
dotnet user-secrets init

# API 키 추가
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "YOUR_GOOGLE_BOOKS_API_KEY"
dotnet user-secrets set "ExternalApis:Kakao:ApiKey" "YOUR_KAKAO_API_KEY"
dotnet user-secrets set "ExternalApis:Aladin:ApiKey" "YOUR_ALADIN_API_KEY"
dotnet user-secrets set "ExternalApis:TMDb:ApiKey" "YOUR_TMDB_API_KEY"
dotnet user-secrets set "ExternalApis:OMDb:ApiKey" "YOUR_OMDB_API_KEY"
dotnet user-secrets set "ExternalApis:Discogs:Token" "YOUR_DISCOGS_TOKEN"

cd ../..
```

### 7.3 appsettings.json 구조

**src/CollectionServer.Api/appsettings.json**:

```json
{
  "ExternalApis": {
    "GoogleBooks": {
      "BaseUrl": "https://www.googleapis.com/books/v1/",
      "ApiKey": "PLACEHOLDER"
    },
    "Kakao": {
      "BaseUrl": "https://dapi.kakao.com/v3/search/",
      "ApiKey": "PLACEHOLDER"
    },
    "Aladin": {
      "BaseUrl": "http://www.aladin.co.kr/ttb/api/",
      "ApiKey": "PLACEHOLDER"
    },
    "TMDb": {
      "BaseUrl": "https://api.themoviedb.org/3/",
      "ApiKey": "PLACEHOLDER"
    },
    "OMDb": {
      "BaseUrl": "http://www.omdbapi.com/",
      "ApiKey": "PLACEHOLDER"
    },
    "MusicBrainz": {
      "BaseUrl": "https://musicbrainz.org/ws/2/",
      "UserAgent": "CollectionServer/1.0.0 (your-email@example.com)"
    },
    "Discogs": {
      "BaseUrl": "https://api.discogs.com/",
      "Token": "PLACEHOLDER"
    }
  }
}
```

---

## 8. 테스트 실행

### 8.1 단위 테스트

```bash
cd tests/CollectionServer.UnitTests
dotnet test --verbosity normal
cd ../..
```

### 8.2 통합 테스트

```bash
cd tests/CollectionServer.IntegrationTests
dotnet test --verbosity normal
cd ../..
```

### 8.3 전체 솔루션 테스트

```bash
dotnet test
```

---

## 9. Podman 빌드 및 실행 (선택)

### 9.1 Containerfile 생성

**Containerfile** (프로젝트 루트):

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/CollectionServer.Api/CollectionServer.Api.csproj", "src/CollectionServer.Api/"]
COPY ["src/CollectionServer.Core/CollectionServer.Core.csproj", "src/CollectionServer.Core/"]
COPY ["src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj", "src/CollectionServer.Infrastructure/"]
RUN dotnet restore "src/CollectionServer.Api/CollectionServer.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/CollectionServer.Api"
RUN dotnet build "CollectionServer.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CollectionServer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CollectionServer.Api.dll"]
```

### 9.2 Podman 이미지 빌드

```bash
podman build -t collectionserver:latest .
```

### 9.3 podman-compose로 전체 스택 실행

**podman-compose.yml** (전체 버전):

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: collectiondb
    environment:
      POSTGRES_DB: collectiondb
      POSTGRES_USER: collectionuser
      POSTGRES_PASSWORD: YourSecurePassword123!
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U collectionuser"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build: .
    container_name: collectionserver_api
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=collectiondb;Username=collectionuser;Password=YourSecurePassword123!
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped

volumes:
  postgres_data:
```

```bash
# podman-compose로 전체 스택 시작
podman-compose up --build

# 또는 Podman pod로 수동 실행
podman pod create --name collectionserver -p 8080:8080 -p 5432:5432
podman run -d --pod collectionserver \
  --name collectiondb \
  -e POSTGRES_DB=collectiondb \
  -e POSTGRES_USER=collectionuser \
  -e POSTGRES_PASSWORD=YourSecurePassword123! \
  -v postgres_data:/var/lib/postgresql/data \
  postgres:16

podman run -d --pod collectionserver \
  --name collectionserver_api \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=collectiondb;Username=collectionuser;Password=YourSecurePassword123! \
  collectionserver:latest

# 백그라운드 실행 (podman-compose)
podman-compose up -d --build

# 로그 확인
podman logs -f collectionserver_api

# 중지
podman-compose down
# 또는 pod 중지
podman pod stop collectionserver
podman pod rm collectionserver
```

---

## 10. 개발 도구 및 확장

### Visual Studio Code 확장

- **C# Dev Kit**: C# 개발 지원
- **REST Client**: HTTP 요청 테스트
- **Podman**: Podman 컨테이너 관리
- **PostgreSQL**: 데이터베이스 탐색

### Visual Studio 2022

- **.NET 10 워크로드**: ASP.NET 및 웹 개발
- **SQL Server 관리 도구**: 데이터베이스 탐색

---

## 문제 해결

### PostgreSQL 연결 오류

```bash
# PostgreSQL 실행 확인
sudo systemctl status postgresql  # Linux
brew services list  # macOS

# 포트 충돌 확인
lsof -i :5432

# Podman 컨테이너 확인
podman ps -a
podman logs collectiondb
```

### EF Core 마이그레이션 오류

```bash
# 마이그레이션 목록 확인
dotnet ef migrations list --startup-project src/CollectionServer.Api

# 마이그레이션 제거 (최신부터)
dotnet ef migrations remove --startup-project src/CollectionServer.Api

# 데이터베이스 삭제 후 재생성
dotnet ef database drop --startup-project src/CollectionServer.Api --force
dotnet ef database update --startup-project src/CollectionServer.Api
```

### 패키지 복원 오류

```bash
# NuGet 캐시 삭제
dotnet nuget locals all --clear

# 재복원
dotnet restore --force
```

---

## 다음 단계

1. ✅ **Phase 0 완료**: research.md - 기술 스택 연구
2. ✅ **Phase 1 완료**: data-model.md, contracts/, quickstart.md
3. ⏭️ **Phase 2**: tasks.md - 구현 작업 분해 (`/speckit.tasks` 명령)
4. ⏭️ **구현**: 실제 코드 작성 및 테스트
5. ⏭️ **배포**: Kubernetes 또는 클라우드 플랫폼

---

**Quickstart 가이드 완료** ✅

문의사항: support@collectionserver.example
