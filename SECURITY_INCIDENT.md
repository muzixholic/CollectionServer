# 🚨 보안 사고 대응 가이드

**날짜**: 2025-11-19  
**사고 유형**: API 키 GitHub 노출

## 🔥 발생한 문제

### 노출된 정보
- **파일**: `podman-compose.yml`
- **커밋**: `20f5bab`, `82d8b90`
- **브랜치**: `001-isbn-book-api`

### 노출된 API 키
1. ❌ Google Books API: `AIzaSyCEMeUgeqh6E_JXK7QTvIdO41CpzRhukWA`
2. ❌ Kakao Book API: `f661a532addc0622d536fb30f4c74022`
3. ❌ Aladin API: `ttbchm16101614002`

## ✅ 즉시 조치 사항

### 1. API 키 재발급 (최우선!)

#### Google Books API
```
1. https://console.cloud.google.com/apis/credentials
2. 기존 키 찾기 → 삭제 또는 비활성화
3. "CREATE CREDENTIALS" → "API key" 클릭
4. 새 키 발급 받기
5. Application restrictions 설정 (HTTP referrers 또는 IP addresses)
```

#### Kakao Book API
```
1. https://developers.kakao.com/console/app
2. 내 애플리케이션 선택
3. "앱 키" 탭에서 REST API 키 확인
4. 필요시 앱 재생성 또는 플랫폼 설정으로 제한
```

#### Aladin API (TTB Key)
```
1. http://www.aladin.co.kr/ttb/wblog_manage.aspx
2. TTB 키 관리
3. 기존 키 삭제 (가능한 경우)
4. 새 TTB 키 발급
```

### 2. 로컬 설정 업데이트

```bash
# 1. .env 파일에 새 API 키 입력
nano .env

# 2. User Secrets도 업데이트
cd src/CollectionServer.Api
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "NEW_GOOGLE_KEY"
dotnet user-secrets set "ExternalApis:KakaoBook:ApiKey" "NEW_KAKAO_KEY"
dotnet user-secrets set "ExternalApis:AladinApi:ApiKey" "NEW_ALADIN_KEY"

# 3. 컨테이너 재시작
cd ../..
podman-compose down
podman-compose up -d
```

### 3. Git 히스토리 정리 (선택)

**Option A: 간단한 방법 (권장)**
```bash
# 새 커밋으로 키 제거 (이미 완료됨)
git add .
git commit -m "security: Remove exposed API keys, use .env file"
git push
```

**Option B: 히스토리 완전 제거 (복잡)**
```bash
# ⚠️ 위험! 공동 작업 중이면 하지 마세요
# BFG Repo-Cleaner 사용
java -jar bfg.jar --replace-text passwords.txt .git
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git push --force
```

## 📝 수정 완료 사항

### ✅ 코드 변경
1. **podman-compose.yml** - API 키 제거, `.env` 파일 사용
2. **.env** - API 키 보관 (gitignore에 포함)
3. **.env.example** - 템플릿 파일 생성
4. **.gitignore** - `.env` 무시 확인 (이미 포함됨)

### ✅ 보안 강화
```yaml
# Before (보안 취약)
environment:
  - ExternalApis__GoogleBooks__ApiKey=ACTUAL_KEY  # ❌ 노출됨

# After (안전)
env_file:
  - .env  # ✅ .gitignore에 포함, Git 추적 안됨
```

## 🛡️ 앞으로의 보안 수칙

### 절대 하지 말 것
- ❌ API 키를 코드에 직접 작성
- ❌ `.env` 파일을 Git에 커밋
- ❌ 설정 파일에 실제 키 포함
- ❌ 스크린샷에 키 노출
- ❌ 로그에 키 출력

### 반드시 할 것
- ✅ `.env` 파일 사용
- ✅ `.env`를 `.gitignore`에 추가
- ✅ `.env.example` 템플릿 제공
- ✅ User Secrets 사용 (로컬 개발)
- ✅ 환경 변수 사용 (컨테이너/프로덕션)
- ✅ API 키 접근 제한 설정 (IP, Domain)
- ✅ 정기적으로 키 rotate

### 추천 도구
```bash
# Git 커밋 전 자동 검사
npm install -g git-secrets
git secrets --install
git secrets --register-aws

# 또는 pre-commit hooks
pip install pre-commit
pre-commit install
```

## 📊 영향 범위

### 잠재적 위험
1. **Google Books API**
   - 할당량 도용 가능
   - 무료 한도: 1,000 requests/day
   - 금전적 손실 가능성: 낮음 (무료 티어)

2. **Kakao Book API**
   - 할당량 도용 가능
   - 무료 한도: 300,000 requests/day
   - 금전적 손실 가능성: 낮음 (무료 서비스)

3. **Aladin API**
   - 할당량 도용 가능
   - 무료 한도: 5,000 requests/day
   - 금전적 손실 가능성: 낮음 (무료 서비스)

### 모니터링
```bash
# API 사용량 확인
# Google Cloud Console - APIs & Services - Credentials
# Kakao Developers - 내 애플리케이션 - 통계
# Aladin - TTB 관리 페이지
```

## 🎯 체크리스트

### 즉시 (5분 내)
- [ ] Google API 키 재발급
- [ ] Kakao API 키 확인/재발급
- [ ] Aladin TTB 키 재발급
- [ ] 기존 키 무효화

### 단기 (오늘 내)
- [x] podman-compose.yml에서 키 제거
- [x] .env 파일 생성
- [x] .env.example 생성
- [x] .gitignore 확인
- [ ] 새 키로 .env 업데이트
- [ ] User Secrets 업데이트
- [ ] 테스트 실행

### 중기 (이번 주)
- [ ] API 키 사용량 모니터링
- [ ] 비정상 사용 패턴 확인
- [ ] 키 rotate 주기 설정
- [ ] Pre-commit hook 설치

## 📞 참고 링크

- [Google Cloud - API Key Best Practices](https://cloud.google.com/docs/authentication/api-keys)
- [GitHub - Removing sensitive data](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
- [OWASP - API Security](https://owasp.org/www-project-api-security/)

---

**⚠️ 중요**: 이 문서는 Git에 커밋하지 마세요!
실제 API 키가 포함되어 있습니다!
