# 외부 API Provider 구현 현황 (2025-11-27)

## 요약
- ✅ 총 8개 Provider 중 6개는 프로덕션 수준(도서 3, 음악 2, 영화 UPC 브리지 1)으로 동작합니다.
- ⚠️ TMDb / OMDb Provider는 UPC 직접 조회를 지원하지 않아 Stub 상태이며, 로그를 남기고 자연스럽게 폴백합니다.
- 🧠 UpcItemDb + TMDb 브리지 Provider가 영화 UPC/EAN-13을 처리하여 TMDb 메타데이터를 반환합니다.
- 🔐 API 키는 `.env`, `.env.prod`, `dotnet user-secrets` 등 외부 비밀 저장소를 통해 주입하며 Git에는 커밋하지 않습니다.

## Provider 매트릭스
| 미디어 | Provider | 상태 | 인증 | 지원 바코드 | 비고 |
| --- | --- | --- | --- | --- | --- |
| 도서 | GoogleBooksProvider | ✅ 운영 | API Key (선택) | ISBN-10/13 | 글로벌 데이터, 표지/장르/페이지 포함 |
| 도서 | KakaoBookProvider | ✅ 운영 | Kakao REST API Key | ISBN-10/13 | 한국 도서 우선, `Authorization: KakaoAK` |
| 도서/음반/DVD | AladinApiProvider | ✅ 운영 | TTB Key | ISBN-10/13, UPC, EAN-13 | `mallType` 값으로 Book/Music/DVD 분기 |
| 음악 | MusicBrainzProvider | ✅ 운영 | User-Agent 필수 | UPC/EAN-13 | 트랙 리스트와 레이블 정보 제공 |
| 음악 | DiscogsProvider | ✅ 운영 | Token + User-Agent | UPC/EAN-13 | 2단계 검색(바코드 검색 → Release 상세) |
| 영화 | UpcItemDbProvider (UpcItemDb + TMDb) | ✅ 운영 | UpcItemDb 공개 API, TMDb Key | UPC/EAN-13 (ISBN 제외) | UPCitemdb의 제목을 TMDb 검색 결과와 매칭 |
| 영화 | TMDbProvider | ⚠️ Stub | API Key | UPC/EAN-13 (ISBN 제외) | 바코드 검색 미지원 → 현재는 경고 로그 후 null 반환 |
| 영화 | OMDbProvider | ⚠️ Stub | API Key | UPC 12자리 | UPC→IMDb 매핑 부재로 Stub 유지 |

## 폴백 순서
- **도서**: GoogleBooks (1) → KakaoBook (2) → Aladin (3)
- **음악**: MusicBrainz (1) → Discogs (2) → Aladin (3, mallType=MUSIC)
- **영화**: UpcItemDb+TMDb (2) → TMDb Stub (3) → OMDb Stub (4) → Aladin (5, mallType=DVD)

`MediaService`는 Provider 등록 수, `SupportsBarcode` 결과, 우선순위, 성공 여부를 모두 로그로 남기며 성공 시 DB와 캐시에 동시에 저장합니다.

## 설정 방법
### 1. User Secrets (개발 환경)
```bash
cd src/CollectionServer.Api
dotnet user-secrets init

# 도서
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "..."
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey"   "..."
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey"   "..."

# 음악
dotnet user-secrets set "ExternalApis:MusicBrainz:UserAgent" "CollectionServer/1.0 (contact@example.com)"
dotnet user-secrets set "ExternalApis:Discogs:ApiKey"       "<token>"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret"    "<secret>"

# 영화
dotnet user-secrets set "ExternalApis:TMDb:ApiKey"         "..."
dotnet user-secrets set "ExternalApis:OMDb:ApiKey"         "..."
# UpcItemDb는 공개 Trial API를 사용하지만, 상용 플랜 사용 시 `ExternalApis:UpcItemDb` 섹션에 키를 추가하세요.
```

### 2. `.env` / `.env.prod`
- `.env.example`: 개발/`podman-compose` 용 API 키 템플릿
- `.env.prod.example`: `docker-compose.prod.yml`에서 사용하는 DB·API 키 템플릿
- Compose 파일은 `ExternalApis__{Provider}__ApiKey` 형식의 환경 변수를 자동으로 주입합니다.

### 3. `appsettings.json` 확장 예시 (UpcItemDb)
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
- `tests/CollectionServer.UnitTests/ExternalApis/*ProviderTests.cs`에서 `SupportsBarcode`, 우선순위, DTO 매핑을 검증합니다.
- `tests/CollectionServer.IntegrationTests`는 Mock HTTP Handler로 Provider 호출을 시뮬레이션하여 NotFound, 폴백, 오류 케이스를 재현합니다.
- `dotnet test` 전체 실행 시 Provider 테스트를 포함해 **280개** 테스트가 실행됩니다.

## 알려진 제한 사항
1. **TMDb/OMDb Stub** – UPC 검색 미지원으로 현재는 로그 후 null 반환. UPC→IMDb 매핑 서비스 도입 전까지 UpcItemDb 브리지에 의존합니다.
2. **Aladin API 트랙 정보 부족** – mallType=MUSIC에서도 트랙 정보가 제공되지 않아 빈 배열을 반환합니다(트랙 정보는 MusicBrainz/Discogs가 채움).
3. **Discogs Rate Limit** – 비인증 상태 60 req/min, Token 사용 시 90 req/min으로 제한됩니다. 애플리케이션 RateLimiter(100 req/min)와 충돌하지 않도록 주의하세요.

## 향후 계획
- 상용 UPC 데이터셋을 조사하여 TMDb/OMDb Provider를 완전 구현합니다.
- Provider별 성공률·지연 시간 지표를 Prometheus/Serilog Sink로 수집합니다.
- `ExternalApis` 설정에 대한 Configuration Binding 테스트를 추가해 잘못된 우선순위/타임아웃 값을 조기에 탐지합니다.
