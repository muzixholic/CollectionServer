# 외부 API Provider 구현 현황 (2025-11-27)

## 요약
- ✅ **6/8 providers** 는 프로덕션 수준으로 동작 (도서 3, 음악 2, UPC 브리지 1).
- ⚠️ **TMDb / OMDb** 는 여전히 직접 UPC 검색을 지원하지 않아 Stub 상태로 유지 (로그와 graceful fallback 제공).
- 🧠 **UpcItemDb + TMDb 브리지** 가 영화 UPC/EAN-13을 처리하여 TMDb 메타데이터를 반환.
- 🔐 API 키는 `.env` / `.env.prod` / `dotnet user-secrets` 를 통해 주입하며 Git에는 저장하지 않습니다.

## Provider 매트릭스
| Media | Provider | 상태 | 인증 | 지원 바코드 | 비고 |
| --- | --- | --- | --- | --- | --- |
| Books | GoogleBooksProvider | ✅ Production | API Key (선택) | ISBN-10/13 | 국제 데이터, 표지/장르/페이지 |
| Books | KakaoBookProvider | ✅ Production | Kakao REST API Key | ISBN-10/13 | 한국 서적, Authorization 헤더 사용 |
| Books / Music / DVD | AladinApiProvider | ✅ Production | TTB Key | ISBN-10/13, UPC, EAN-13 | `mallType` 으로 Book/Music/DVD 자동 매핑 |
| Music | MusicBrainzProvider | ✅ Production | User-Agent 필수 | UPC/EAN-13 | 트랙리스트 + 레이블 제공 |
| Music | DiscogsProvider | ✅ Production | Token + User-Agent | UPC/EAN-13 | 2-step (search → release) + 트랙 정보 |
| Movies | UpcItemDbProvider (UpcItemDb + TMDb) | ✅ Production | UpcItemDb: 공개, TMDb: API Key | UPC/EAN-13 (ISBN 제외) | UPCitemdb로 제목 확보 후 TMDb ID/상세 조회 |
| Movies | TMDbProvider | ⚠️ Stub | API Key | UPC/EAN-13 (ISBN 제외) | TMDb는 UPC 검색을 지원하지 않아 현재는 로그 후 null 반환 |
| Movies | OMDbProvider | ⚠️ Stub | API Key | UPC (12자리) | UPC→IMDb 매핑 부재로 Stub 유지 |

## 폴백 체인
- **Books**: GoogleBooks (1) → KakaoBook (2) → Aladin (3)
- **Music**: MusicBrainz (1) → Discogs (2) → Aladin (3, mallType=MUSIC 시)
- **Movies**: UpcItemDb+TMDb (2) → TMDb Stub (3) → OMDb Stub (4) → Aladin (5, mallType=DVD)

`MediaService` 는 Provider 등록 수와 `SupportsBarcode` 결과를 모두 로그로 남기며, 성공 시 DB + 캐시에 저장합니다.

## 설정 방법
### 1. User Secrets (개발 환경)
```bash
cd src/CollectionServer.Api
dotnet user-secrets init

# Books
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "..."
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey"   "..."
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey"   "..."

# Music
dotnet user-secrets set "ExternalApis:MusicBrainz:UserAgent" "CollectionServer/1.0 (contact@example.com)"
dotnet user-secrets set "ExternalApis:Discogs:ApiKey"       "<token>"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret"    "<secret>"

# Movies
dotnet user-secrets set "ExternalApis:TMDb:ApiKey"         "..."
dotnet user-secrets set "ExternalApis:OMDb:ApiKey"         "..."
# UpcItemDb의 trial endpoint는 키가 필요 없지만, 상업 플랜을 사용할 경우 ExternalApis:UpcItemDb 섹션에 설정하세요.
```

### 2. `.env` / `.env.prod`
- `.env.example` : 로컬 / `podman-compose` 용 API 키 템플릿.
- `.env.prod.example` : `docker-compose.prod.yml` 에서 사용하는 DB + API 키 템플릿.
- Compose 파일들은 `ExternalApis__{Provider}__*` 환경 변수를 자동으로 주입합니다.

### 3. appsettings 확장 (UpcItemDb 예시)
```json
"ExternalApis": {
  "UpcItemDb": {
    "BaseUrl": "https://api.upcitemdb.com/prod/trial",
    "Priority": 2,
    "TimeoutSeconds": 10
  }
}
```

## 테스트 전략
- `tests/CollectionServer.UnitTests/ExternalApis/*ProviderTests.cs` 에서 SupportsBarcode/우선순위/DTO 매핑 검증 (63개 이상의 provider 테스트 포함).
- `tests/CollectionServer.IntegrationTests` 는 Mock HTTP 핸들러로 provider 호출을 시뮬레이션하여 NotFound, fallback, 오류 케이스를 재현.
- 전체 스위트 (`dotnet test`) 는 280개의 테스트를 실행하며 provider 테스트도 포함됩니다.

## 알려진 제한 사항
1. **TMDb/OMDb Stub** – UPC 기반 검색이 지원되지 않아 로그 후 null 반환. UPC→IMDb 매핑 서비스 도입 전까지 UpcItemDb 브리지에 의존합니다.
2. **Aladin API 트랙 정보** – MallType=MUSIC 시에도 트랙 목록이 제공되지 않아 빈 배열로 응답합니다 (MusicBrainz/Discogs가 트랙을 채움).
3. **Discogs Rate Limit** – 비인증 상태에서는 60 req/분, 토큰 사용 시 90 req/분; RateLimiter 설정(100/분)과 충돌하지 않도록 주의하세요.

## 향후 계획
- 상용 UPC 데이터셋 조사 후 TMDb/OMDb provider 완전 구현.
- Provider별 성공률/지연시간 메트릭을 Prometheus/Serilog sink로 수집.
- `ExternalApis` 설정을 ConfigurationBinding 테스트로 검증하여 잘못된 우선순위/timeout 값을 조기에 잡아냅니다.
