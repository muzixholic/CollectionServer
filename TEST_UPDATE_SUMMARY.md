# Provider 테스트 업데이트 완료 요약

**날짜**: 2025-11-19  
**상태**: ✅ **완료** (63/63 tests passing)
> 2025-11-27 기준: Provider 테스트는 전체 `dotnet test` 파이프라인(총 280개 테스트)의 일부로 계속 실행되고 있으며 최신 실행에서도 모두 통과했습니다.

## 수정 사항
### Provider 코드 변경
1. **MusicBrainzProvider** – `SupportsBarcode` 로직에서 ISBN 접두(978/979) 12자리를 제외하도록 수정
2. **TMDbProvider / DiscogsProvider** – 위와 동일한 로직으로 UPC만 허용
3. **OMDbProvider** – API 제약에 맞춰 12자리 UPC만 지원하도록 단순화

### 테스트 코드 변경
- MusicBrainz/TMDb/Discogs 테스트의 잘못된 12자리 샘플을 13자리 ISBN(978/979)으로 교체하여 명확한 실패 조건을 검증
- OMDb 테스트는 12자리만 성공, 13자리/ISBN 케이스는 실패하도록 시나리오 정리

## 결과
- Provider 단위 테스트 63개 전부 통과
- 전체 테스트(`dotnet test`) 실행 시 Provider 테스트 포함 총 280개가 통과

## 배운 점
- UPC와 ISBN 접두의 구분을 명확히 해야 Provider 간 지원 범위를 혼동하지 않습니다.
- API 제약(OMDb 12자리, TMDb UPC 미지원 등)을 테스트에 반영해야 CI에서 일관된 결과를 얻을 수 있습니다.
