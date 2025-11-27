# 🚨 보안 사고 보고서 – API 키 노출 (업데이트: 2025-11-27)

## 사건 개요
- **사건**: 개발 중 `podman-compose.yml` 에 실제 API 키가 실수로 커밋되어 GitHub에 노출됨.
- **영향**: Google Books, Kakao Book, Aladin TTB 키 (값은 즉시 삭제됨).
- **조치**: 문제 커밋 정리, `.env` 템플릿 추가, Compose 파일에서 키 제거, 새 키 발급.

## 수행한 대응
1. 문제 커밋에서 민감 정보 제거 및 Force Push 없이 최신 커밋에서만 수정 (히스토리 보존).
2. `.env`, `.env.example`, `.env.prod.example` 작성 후 `env_file` 또는 `--env-file` 로만 키를 주입하도록 변경.
3. GitHub Actions / Compose 문서에 "환경 변수로만 전달" 규칙 명시.
4. 영향 받은 모든 키 **즉시 회수 & 재발급** (Google/Kakao/Aladin 포털).
5. Repository 전반에 GitHub Secret scanning (기본 정책) 및 수동 `git secrets` 설치 가이드 공유.

## 남은 할 일
- [ ] 주기적인 키 로테이션 일정 수립 (분기별 권장).
- [ ] GitHub 보호 규칙에 secret scanning required 상태 적용.
- [ ] PR 체크리스트에 "환경 변수/Secrets 사용" 항목 명시.

## 재발 방지 수칙
- API 키/비밀번호/토큰은 **오직** `dotnet user-secrets`, `.env`, GitHub Secrets, 또는 배포용 secret manager에만 저장합니다.
- `.env` 파일은 `.gitignore` 에 포함되어 있으며, 팀과 공유할 때는 암호화된 채널을 사용합니다.
- 로그/스크린샷/문서에 민감 값이 노출되지 않았는지 리뷰합니다.
- PR 템플릿/코드 리뷰 단계에서 secret 존재 여부 확인.

## 참고 자료
- [Google Cloud – API Key 사용 모범사례](https://cloud.google.com/docs/authentication/api-keys)
- [Kakao Developers – 앱 키 관리](https://developers.kakao.com/)
- [GitHub – 민감 정보 제거](https://docs.github.com/en/secure-development/)
