# API 키 설정 & 검증 가이드 (2025-11-27)

## 현재 상태
- `.env`, `.env.example`, `.env.prod.example` 템플릿이 존재하며 Git에 커밋되지 않습니다.
- 로컬 개발은 `dotnet user-secrets` 를 사용해 모든 Provider 키/토큰을 주입합니다.
- CI/CD는 GitHub Secrets → Actions → GHCR 이미지 → `docker-compose.prod.yml` 로 이어지는 흐름이며, 실행 시 `.env.prod` 를 통해 환경 변수를 공급합니다.
- Provider 호출 성공/실패 여부는 `MediaService` 로그에서 실시간으로 확인할 수 있습니다.

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
`dotnet user-secrets list` 로 확인할 수 있으며, 값은 암호화된 User Secrets 저장소에만 존재합니다.

### 2. `.env` (컨테이너 개발)
1. `cp .env.example .env`
2. 필요한 키를 채운 후 `podman-compose up -d`
3. Compose가 `ExternalApis__{Provider}__ApiKey` 환경 변수를 API 컨테이너에 주입합니다.

### 3. `.env.prod` (프로덕션)
1. `cp .env.prod.example .env.prod`
2. DB 자격 증명 + 모든 API 키 입력
3. `docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d`

### 4. GitHub Actions (CI/CD)
- GitHub Secrets 예시: `GOOGLE_BOOKS_API_KEY`, `KAKAO_API_KEY`, `TMDB_API_KEY` 등
- `.github/workflows/ci-cd.yml` 에서 이미지 빌드 후 GHCR에 Push → 서버에서는 `.env.prod` 로 주입

## 검증 체크리스트
1. `dotnet user-secrets list` 또는 `printenv | grep ExternalApis` 로 값 확인
2. `dotnet run` 실행 후 `curl http://localhost:5000/items/<barcode>`
3. 로그에서 다음 항목 확인:
   - `Total providers registered: 8`
   - `Provider {Name} supports barcode ...: True`
   - `Trying provider {Name}` → `Successfully retrieved ...`
4. 실패 시 `Authorization` 헤더(예: Kakao `KakaoAK <key>`) 또는 QueryString 키(Aladin `ttbkey`) 가 포함되었는지 확인

## 트러블슈팅
| 증상 | 체크 포인트 |
| --- | --- |
| Provider가 호출되지 않음 | `MediaService` 로그로 `SupportsBarcode` 여부 확인, `ExternalApis:{Provider}` 섹션이 비어 있지 않은지 확인 |
| 401/403 응답 | 키가 잘못되었거나, Kakao/Discogs 처럼 User-Agent/Tokem이 누락되었는지 확인 |
| 404 응답만 반복 | 실제로 해당 데이터가 없거나, TMDb/OMDb Stub가 호출된 상황 – UpcItemDb 브리지가 선행 호출되었는지 확인 |
| 컨테이너에서 키 미주입 | `.env` 파일이 `podman-compose.yml` 의 `env_file` 로 참조되는지, 변수명이 `ExternalApis__` 포맷인지 확인 |

## 보안 수칙
- API 키를 코드/커밋/스크린샷에 절대 남기지 마세요.
- `.env` / `.env.prod` 는 `.gitignore` 로 보호되며, 공유 시 암호화된 채널을 사용하세요.
- 노출 사고 발생 시 즉시 키 재발급 → `.env` / User Secrets 업데이트 → 컨테이너 재시작.
