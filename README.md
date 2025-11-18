# CollectionServer - 미디어 정보 API 서버

다양한 미디어 유형(도서, Blu-ray, DVD, 음악 앨범)의 바코드/ISBN을 입력받아 해당 미디어의 상세 정보를 반환하는 통합 REST API 서버입니다.

## 기술 스택

- **언어**: C# 13
- **프레임워크**: ASP.NET Core 10.0
- **데이터베이스**: PostgreSQL 16+
- **ORM**: Entity Framework Core 10.0
- **컨테이너**: Podman
- **테스트**: xUnit, Moq, FluentAssertions

## 주요 기능

- 바코드/ISBN 기반 미디어 정보 조회 (ISBN-10, ISBN-13, UPC, EAN-13 지원)
- Database-First 아키텍처: PostgreSQL 우선 조회 → 외부 API 폴백
- 다양한 외부 데이터 소스 통합 (Google Books, TMDb, MusicBrainz 등)
- 우선순위 기반 폴백 전략
- Rate Limiting (100 req/min)
- OpenAPI/Swagger 문서화

## 프로젝트 구조

```
CollectionServer/
├── src/
│   ├── CollectionServer.Api/           # ASP.NET Core Web API
│   ├── CollectionServer.Core/          # 도메인 레이어
│   └── CollectionServer.Infrastructure/ # 인프라 레이어
├── tests/
│   ├── CollectionServer.UnitTests/
│   ├── CollectionServer.IntegrationTests/
│   └── CollectionServer.ContractTests/
└── specs/                              # 기능 명세서
```

## 시작하기

### 사전 요구사항

- .NET SDK 10.0.100+
- PostgreSQL 16+
- Podman (선택사항, 컨테이너 실행 시)

### 로컬 실행

1. 리포지토리 클론:
```bash
git clone <repository-url>
cd CollectionServer
```

2. 데이터베이스 연결 문자열 설정:
```bash
cd src/CollectionServer.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=collectionserver;Username=postgres;Password=yourpassword"
```

3. 데이터베이스 마이그레이션 적용:
```bash
dotnet ef database update
```

4. API 서버 실행:
```bash
dotnet run --project src/CollectionServer.Api
```

5. Swagger UI 접근:
```
http://localhost:5000/swagger
```

### Podman으로 실행

```bash
podman-compose up -d
```

API 서버: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

## API 엔드포인트

### 미디어 정보 조회

```
GET /items/{barcode}
```

**파라미터**:
- `barcode`: ISBN-10, ISBN-13, UPC, 또는 EAN-13 바코드

**응답 예시**:
```json
{
  "id": "uuid",
  "barcode": "9788950991234",
  "mediaType": "Book",
  "title": "책 제목",
  "description": "책 설명",
  "imageUrl": "https://...",
  "source": "GoogleBooks"
}
```

### 헬스 체크

```
GET /health
```

## 테스트 실행

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트만 테스트
dotnet test tests/CollectionServer.UnitTests
dotnet test tests/CollectionServer.IntegrationTests
dotnet test tests/CollectionServer.ContractTests
```

## 설정

### 외부 API 키 설정

외부 데이터 소스를 사용하려면 API 키를 User Secrets에 저장하세요:

```bash
cd src/CollectionServer.Api
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "your-api-key"
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
