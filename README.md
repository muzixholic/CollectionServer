# CollectionServer - 미디어 정보 API 서버

다양한 미디어 유형(도서, Blu-ray, DVD, 음악 앨범)의 바코드/ISBN을 입력받아 해당 미디어의 상세 정보를 반환하는 통합 REST API 서버입니다.

## 기술 스택

- **언어**: C# 13
- **프레임워크**: ASP.NET Core 10.0 (Minimal API)
- **데이터베이스**: PostgreSQL 16+
- **ORM**: Entity Framework Core 10.0
- **컨테이너**: Podman
- **테스트**: xUnit, Moq, FluentAssertions
- **로깅**: Serilog
- **문서화**: OpenAPI/Swagger

## 주요 기능

### ✨ 핵심 기능
- **바코드/ISBN 기반 미디어 정보 조회**: ISBN-10, ISBN-13, UPC, EAN-13 지원
- **Database-First 아키텍처**: PostgreSQL 우선 조회 (<500ms) → 외부 API 폴백 (<2초)
- **다양한 외부 데이터 소스 통합**:
  - 도서: Google Books, Kakao Book, Aladin API
  - 영화: TMDb, OMDb
  - 음악: MusicBrainz, Discogs
- **우선순위 기반 폴백 전략**: 1차 실패 시 자동으로 2차, 3차 소스 시도
- **Rate Limiting**: 100 req/min (프로덕션: 200 req/min)
- **한국어 오류 메시지**: 사용자 친화적인 오류 응답
- **OpenAPI/Swagger 문서화**: 인터랙티브 API 문서

### 🚀 성능 최적화
- 데이터베이스 연결 풀링 (Max 100)
- 쿼리 최적화 (AsNoTracking, Compiled Queries)
- 바코드 및 미디어 타입 인덱스
- 응답 시간 로깅

## 프로젝트 구조

```
CollectionServer/
├── src/
│   ├── CollectionServer.Api/           # ASP.NET Core Web API
│   ├── CollectionServer.Core/          # 도메인 레이어 (Entities, Services, Interfaces)
│   └── CollectionServer.Infrastructure/ # 인프라 레이어 (Repositories, External APIs)
├── tests/
│   ├── CollectionServer.UnitTests/      # 단위 테스트
│   ├── CollectionServer.IntegrationTests/ # 통합 테스트
│   └── CollectionServer.ContractTests/  # 계약 테스트
├── specs/                              # 기능 명세서
├── Containerfile                       # Podman/Docker 이미지
└── podman-compose.yml                 # 전체 스택 구성
```

## 시작하기

### 사전 요구사항

- .NET SDK 10.0.100+
- PostgreSQL 16+
- Podman (선택사항, 컨테이너 실행 시)

### 로컬 실행

#### 1. 리포지토리 클론
```bash
git clone <repository-url>
cd CollectionServer
```

#### 2. 데이터베이스 연결 문자열 설정
```bash
cd src/CollectionServer.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=collectionserver;Username=postgres;Password=yourpassword"
```

#### 3. 외부 API 키 설정 (선택사항)
```bash
# Google Books API
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "your-google-api-key"

# Kakao Book API
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey" "your-kakao-api-key"

# TMDb API
dotnet user-secrets set "ExternalApis:TMDb:ApiKey" "your-tmdb-api-key"

# OMDb API
dotnet user-secrets set "ExternalApis:OMDb:ApiKey" "your-omdb-api-key"

# Discogs API
dotnet user-secrets set "ExternalApis:Discogs:ApiKey" "your-discogs-key"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret" "your-discogs-secret"
```

#### 4. 데이터베이스 마이그레이션 적용
```bash
dotnet ef database update --project src/CollectionServer.Infrastructure --startup-project src/CollectionServer.Api
```

#### 5. API 서버 실행
```bash
dotnet run --project src/CollectionServer.Api
```

#### 6. Swagger UI 접근
```
http://localhost:5000/swagger
```

### Podman으로 실행

전체 스택(PostgreSQL + API)을 컨테이너로 실행:

```bash
podman-compose up -d
```

API 서버: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

중지:
```bash
podman-compose down
```

## API 엔드포인트

### 미디어 정보 조회

```http
GET /items/{barcode}
```

**파라미터**:
- `barcode`: ISBN-10, ISBN-13, UPC, 또는 EAN-13 바코드

**응답 예시 (도서)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "barcode": "9788950991234",
  "barcodeType": "ISBN13",
  "mediaType": "Book",
  "title": "클린 코드",
  "description": "애자일 소프트웨어 장인 정신",
  "imageUrl": "https://example.com/image.jpg",
  "source": "GoogleBooks",
  "authors": ["로버트 C. 마틴"],
  "publisher": "인사이트",
  "publishedDate": "2013-12-24T00:00:00",
  "isbn": "9788950991234",
  "pageCount": 584,
  "language": "ko",
  "createdAt": "2025-11-18T10:00:00Z",
  "updatedAt": "2025-11-18T10:00:00Z"
}
```

**상태 코드**:
- `200 OK`: 성공
- `400 Bad Request`: 잘못된 바코드 형식
- `404 Not Found`: 미디어를 찾을 수 없음
- `429 Too Many Requests`: Rate Limit 초과
- `500 Internal Server Error`: 서버 오류

### 헬스 체크

```http
GET /health
```

**응답**:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-18T10:00:00Z"
}
```

## 테스트 실행

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트만 테스트
dotnet test tests/CollectionServer.UnitTests
dotnet test tests/CollectionServer.IntegrationTests
dotnet test tests/CollectionServer.ContractTests

# 코드 커버리지 포함
dotnet test --collect:"XPlat Code Coverage"
```

## 설정

### Rate Limiting

`appsettings.json`에서 설정 가능:

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  }
}
```

### 데이터베이스 연결 풀링

연결 문자열에 풀링 옵션 추가:

```
Host=localhost;Database=collectionserver;Username=postgres;Password=yourpassword;Maximum Pool Size=100;Connection Idle Lifetime=300
```

### 외부 API 우선순위

각 미디어 타입별로 Provider 우선순위 설정 (낮을수록 높은 우선순위):

```json
{
  "ExternalApis": {
    "GoogleBooks": { "Priority": 1 },
    "KakaoBook": { "Priority": 2 },
    "AladinApi": { "Priority": 3 }
  }
}
dotnet user-secrets set "ExternalApis:TMDb:ApiKey" "your-api-key"
dotnet user-secrets set "ExternalApis:OMDb:ApiKey" "your-api-key"
```

## 라이선스

MIT License

## 기여

기여를 환영합니다! Pull Request를 제출하기 전에 테스트가 통과하는지 확인하세요.

## 문서

자세한 문서는 `specs/` 디렉토리를 참고하세요:
- `specs/001-isbn-book-api/spec.md`: 기능 명세서
- `specs/001-isbn-book-api/plan.md`: 구현 계획
- `specs/001-isbn-book-api/data-model.md`: 데이터 모델
- `specs/001-isbn-book-api/quickstart.md`: 빠른 시작 가이드
