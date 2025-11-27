# API 키 설정 & 검증 가이드 (2025-11-27)

## 현재 상태
- `.env`, `.env.example`, `.env.prod.example` 템플릿이 존재하며 Git에 커밋되지 않습니다.
- 로컬 개발은 `dotnet user-secrets`로 Provider 키/토큰을 주입합니다.
- CI/CD는 GitHub Secrets → GitHub Actions → GHCR 이미지 → `docker-compose.prod.yml` 순으로 값을 전달합니다.
- Provider 호출 성공/실패 여부는 `MediaService` 로그(등록 수, `SupportsBarcode`, Provider 시도 결과)로 즉시 확인할 수 있습니다.

## 구성 절차
### 1. User Secrets (로컬 개발)
```bash
cd src/CollectionServer.Api

dotnet user-secrets init

dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey"   "<google>"
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey"     "<kakao>"
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey"     "<ttb>"
dotnet user-secrets set "ExternalApis:TMDb:ApiKey"          "<tmdb>"
dotnet user-secrets set "ExternalApis:OMDb:ApiKey"          "<omdb>"
dotnet user-secrets set "ExternalApis:Discogs:ApiKey"       "<discogs-token>"
dotnet user-secrets set "ExternalApis:Discogs:ApiSecret"    "<discogs-secret>"
dotnet user-secrets set "ExternalApis:MusicBrainz:UserAgent" "CollectionServer/1.0 (contact@example.com)"
```
`dotnet user-secrets list`로 설정 값을 확인할 수 있으며, 이 값은 로컬 사용자 비밀 저장소에만 존재합니다.

### 2. `.env` (컨테이너 개발)
1. `cp .env.example .env`
2. 필요한 키를 입력한 뒤 `podman-compose up -d`
3. Compose가 `ExternalApis__{Provider}__ApiKey` 형식으로 컨테이너에 주입합니다.

### 3. `.env.prod` (운영)
1. `cp .env.prod.example .env.prod`
2. DB 자격 증명 + 모든 API 키 입력
3. `docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d`

### 4. GitHub Actions
- GitHub Secrets 예시: `GOOGLE_BOOKS_API_KEY`, `KAKAO_API_KEY`, `TMDB_API_KEY`, `OMDB_API_KEY`, `DISCOGS_TOKEN` 등
- `.github/workflows/ci-cd.yml`에서 이미지 빌드 후 GHCR에 Push 하며, 서버에서는 `.env.prod`를 통해 환경 변수를 주입합니다.

## 검증 체크리스트
1. `dotnet user-secrets list` 또는 `printenv | grep ExternalApis`로 값이 설정되었는지 확인합니다.
2. `dotnet run` 실행 후 `curl http://localhost:5000/items/<barcode>`로 실제 조회를 시도합니다.
3. 로그에서 다음 항목을 확인합니다.
   - `Total providers registered: ...`
   - `Provider {Name} supports barcode ...`
   - `Trying provider {Name}` → `Successfully retrieved ...`
4. 실패 시 Authorization 헤더(Kakao), QueryString 키(Aladin `ttbkey`) 등 인증 값이 포함되었는지 다시 확인합니다.

## 트러블슈팅
| 증상 | 점검 포인트 |
| --- | --- |
| Provider가 호출되지 않음 | `ExternalApis:{Provider}` 섹션 누락 여부, `SupportsBarcode` 로그 확인 |
| 401/403 응답 | 잘못된 키 또는 필수 헤더(User-Agent, Token)가 누락되었는지 확인 |
| 404만 반복 | 실제 데이터 부재이거나 TMDb/OMDb Stub이 호출된 상황 – UpcItemDb 브리지 결과를 확인 |
| 컨테이너에서 키 미주입 | `.env` 파일이 `env_file`로 연결되어 있는지, 변수명이 `ExternalApis__` 규칙을 따르는지 확인 |

## 보안 수칙
- API 키는 코드·커밋·스크린샷에 절대 남기지 않습니다.
- `.env` / `.env.prod`는 `.gitignore`로 보호되며, 외부 공유 시 암호화된 채널을 사용합니다.
- 노출 사고 발생 시 즉시 키를 폐기·재발급하고, `.env` / User Secrets / GitHub Secrets 값을 교체한 뒤 컨테이너를 재기동합니다.
