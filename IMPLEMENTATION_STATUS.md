# 구현 현황 (2025-11-27 업데이트)

CollectionServer는 캐시 우선 조회, 외부 Provider 연동, 배포 문서를 포함한 MVP 범위를 완료했으며, 남은 작업은 영화 UPC 직접 지원·Postgres 마이그레이션·운영 모니터링 강화 등 선택적 향상 과제입니다.

## Phase 진행 상황
| Phase | 범위 | 상태 | 메모 |
| --- | --- | --- | --- |
| 1. Setup | 솔루션 구조, 도구 설정, CI 부트스트랩 | ✅ 완료 | 리포지토리 구조, 분석기, 포맷터, 로깅 기본값 확보 |
| 2. Foundation | 도메인 엔터티, 리포지토리, Validator | ✅ 완료 | 클린 아키텍처 레이어, BarcodeValidator 엣지 케이스, EF 설정 정리 |
| 3. Core Media Query | `/items/{barcode}` 엔드포인트, EdgeCase 테스트 | ✅ 완료 | 96+ 엣지 테스트, Swagger, 한국어 ProblemDetails, `dotnet test` 280개 통과 |
| 4. External Providers | 도서/음악/영화 Provider | ✅ 완료 | Google·Kakao·Aladin·MusicBrainz·Discogs·TMDb·OMDb, UpcItemDbResolver 연동 완료 |
| 5. Error Handling | ProblemDetails, 현지화 | ✅ 완료 | 표준화 미들웨어, 한국어 메시지, 재시도 친화적 응답 |
| 6. Performance | 인덱스, 캐시, 로깅 | ✅ 완료 | Garnet Cache-first, DB 인덱스, 응답 시간 로그, 운영 환경에서 Postgres + Garnet 사용 |
| 7. Resilience & Fallback | 우선순위 체인, 회복탄력성 | ✅ 완료 | `AddStandardResilienceHandler`로 재시도·서킷브레이커·타임아웃 표준화 |
| 8. Rate Limiting | 고정 창 정책 | ✅ 완료 | 개발 100 req/min, 운영 200 req/min, RateLimiting 미들웨어 적용 |
| 9. Deployment | 컨테이너, CI/CD, 문서 | ⚡ 서버 준비 중 | GHCR 배포, prod compose, nginx 템플릿, 배포 가이드 – 실 서버/인증서 준비 필요 |

## 주요 성과
- **Cache → DB → Provider** 파이프라인: Garnet/Redis 캐시에서 1시간 TTL로 저장하고, Miss 시 DB 및 Provider를 순차 조회.
- **외부 Provider 커버리지**: 도서·음악은 운영 수준, 영화는 UpcItemDbResolver + TMDb/OMDb 조합으로 UPC를 직접 처리합니다.
- **관측 가능성**: Serilog 콘솔/파일 로그 + Provider 등록 수, 지원 여부, 시도/성공 로그 제공.
- **안전성**: Rate Limiting, 한국어 ProblemDetails, `.env` + `dotnet user-secrets` 기반 API 키 분리.
- **자동화**: 280개 테스트, CI 파이프라인, podman/docker compose, docs 업데이트 완료.

## 남은 과제
1. **UPC Resolver 정확도 향상** – 상용 UPC→IMDb/TMDb 데이터 또는 추가 Resolver(Source: Catalog, OCR)로 정확도를 높이고 다중 Resolver 체인을 구축합니다.
2. **Postgres 마이그레이션** – 초기 EF Core 마이그레이션을 생성해 Schema를 명시적으로 관리하고 CI/CD에 `dotnet ef database update` 단계를 추가합니다.
3. **운영 모니터링** – Health, Rate Limit, Provider 실패율을 Prometheus/App Insights 등 외부 모니터링과 연계합니다.
4. **비밀 회전 정책** – `.env.prod`와 GitHub Secrets의 로테이션 주기를 문서화하고 자동화 스크립트를 마련합니다.

## 지표 (2025-11-27)
- **테스트**: 280개 통과 (단위 + 통합 + 계약)
- **Provider**: 8개 등록 / 6개 운영 준비
- **지연 목표**: Cache Hit < 5ms, DB Hit < 500ms, Provider < 2s (Resilience 정책으로 강제)
- **배포 산출물**: GHCR `ghcr.io/<repo>/collectionserver:latest`, `docker-compose.prod.yml`, `nginx/conf.d/default.conf`, `.env.prod.example`

## 다음 단계
- UPC Resolver의 다중 데이터 소스(상용 카탈로그, OCR 등)를 조사해 정확도를 개선합니다.
- PostgreSQL 인프라 준비 후 `InitialCreate` 마이그레이션을 커밋하고 CI/CD에 반영합니다.
- 모니터링/알림 시나리오를 `docs/deployment.md`에 추가하고 운영 대시보드를 구성합니다.
