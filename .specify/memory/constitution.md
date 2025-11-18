<!--
Sync Impact Report - Constitution Update
========================================
Version: [Initial] → 1.0.0
Type: MAJOR - Initial constitution ratification
Date: 2025-11-17

Principles Added:
- I. Korean Language Requirement (한국어 필수)
- II. C# and .NET 10 Technology Stack

Sections Added:
- Development Workflow
- Quality Standards

Templates Status:
✅ plan-template.md - Updated with Korean language and C#/.NET requirements
✅ spec-template.md - Updated with Korean language requirements
✅ tasks-template.md - Updated with Korean language and C#/.NET context
✅ agent-file-template.md - Updated with Korean language requirements
✅ checklist-template.md - Updated with Korean language requirements

Follow-up Items:
- None - All templates synchronized
-->

# CollectionServer 프로젝트 헌장 (Project Constitution)

본 문서는 CollectionServer 프로젝트의 핵심 원칙과 거버넌스 규칙을 정의합니다.

## 핵심 원칙 (Core Principles)

### I. 한국어 필수 (Korean Language Requirement)

**원칙**: 모든 프로젝트 결과물 및 문서는 한국어로 작성되어야 합니다.

**적용 범위**:
- 모든 기술 문서 (사양서, 계획서, 작업 목록, 체크리스트)
- API 문서 및 계약서 (contracts)
- 코드 주석 (복잡한 로직 설명 시)
- 커밋 메시지 및 Pull Request 설명
- README 및 사용자 가이드
- 에러 메시지 및 로그 출력

**예외**:
- 프로그래밍 언어 키워드 및 표준 라이브러리 (C# 언어 자체)
- 변수명, 함수명, 클래스명 (영어 사용 권장)
- 외부 라이브러리 및 프레임워크 문서 (원문 그대로)
- 국제 표준 (ISO, RFC 등) 참조

**근거**: 
- 팀원 간 명확한 의사소통 보장
- 도메인 지식의 정확한 표현
- 유지보수 및 인수인계 용이성
- 한국 사용자를 위한 최적화된 사용자 경험

### II. C# 및 .NET 10 기술 스택 (Technology Stack)

**원칙**: 프로젝트의 모든 서버 사이드 코드는 C# 언어와 .NET 10 프레임워크를 사용하여 작성되어야 합니다.

**필수 요구사항**:
- **언어**: C# (최신 언어 기능 활용 권장)
- **프레임워크**: .NET 10
- **ORM**: Entity Framework Core (프레임워크 버전과 일치하는 메이저 버전 사용)
- **프로젝트 형식**: .csproj 기반 프로젝트 구조
- **패키지 관리**: NuGet 사용
- **빌드 도구**: dotnet CLI

**권장 사항**:
- 최신 C# 언어 기능 활용 (레코드 타입, 패턴 매칭, nullable 참조 타입 등)
- 비동기 프로그래밍 (async/await) 적극 활용
- LINQ를 통한 데이터 처리
- 의존성 주입 (Dependency Injection) 패턴 사용
- 최소 API (Minimal API) 또는 ASP.NET Core 사용

**금지 사항**:
- 다른 백엔드 언어 도입 (Python, Java, Node.js 등)
- 구버전 .NET Framework 사용
- 레거시 ASP.NET (MVC 5 이하) 사용

**근거**:
- 타입 안정성과 성능 최적화
- 현대적인 비동기 프로그래밍 지원
- 크로스 플랫폼 호환성
- 풍부한 생태계와 커뮤니티 지원
- 기업급 애플리케이션 개발에 적합
- 장기적인 마이크로소프트 지원 보장

## 개발 워크플로우 (Development Workflow)

### 문서화 우선 접근

모든 기능 개발은 다음 순서를 따라야 합니다:

1. **사양 작성** (spec.md): 사용자 시나리오와 요구사항을 한국어로 명확히 정의
2. **계획 수립** (plan.md): C#/.NET 기반 기술 컨텍스트와 프로젝트 구조 설계
3. **작업 분해** (tasks.md): 구현 가능한 단위 작업으로 분해
4. **구현**: C#으로 코드 작성, 한국어 주석 포함
5. **검증**: 테스트 및 품질 검사

### 코드 리뷰 기준

- **한국어 검증**: 모든 문서와 주석이 올바른 한국어로 작성되었는지 확인
- **기술 스택 준수**: C#/.NET 10 외 다른 기술 사용 여부 검증
- **코드 품질**: C# 코딩 컨벤션 및 베스트 프랙티스 준수
- **테스트 커버리지**: 핵심 로직에 대한 단위 테스트 존재 여부

### 브랜치 전략

- 기능 개발: `[번호]-기능명` 형식 (예: `001-isbn-book-api`)
- 커밋 메시지: 한국어로 작성, 변경 내용 명확히 기술

## 품질 표준 (Quality Standards)

### 코드 품질

- **네이밍**: 영어로 작성된 명확한 변수명, 클래스명 사용
- **주석**: 복잡한 비즈니스 로직은 한국어로 설명
- **예외 처리**: 모든 예외에 한국어 메시지 포함
- **로깅**: 운영 환경에서 디버깅 가능한 한국어 로그 메시지

### 테스트 요구사항

- **단위 테스트**: xUnit 또는 NUnit 사용
- **통합 테스트**: WebApplicationFactory를 활용한 API 테스트
- **테스트 설명**: 테스트 메서드명은 영어, 테스트 시나리오 설명은 한국어 주석

### 문서 품질

- **완결성**: 모든 필수 섹션 작성 완료
- **명확성**: 기술 용어 사용 시 필요한 경우 영문 병기 (예: "의존성 주입 (Dependency Injection)")
- **정확성**: 기술적 오류 없음, 최신 .NET 10 문서 참조

## 거버넌스 (Governance)

### 헌장의 우선순위

이 헌장은 모든 개발 관행과 가이드라인보다 우선합니다. 헌장과 충돌하는 모든 문서나 관행은 헌장에 맞춰 수정되어야 합니다.

### 개정 절차

헌장 개정은 다음 조건 하에 가능합니다:

1. **제안**: 개정이 필요한 명확한 근거 제시
2. **검토**: 개정이 기존 원칙 및 프로젝트 목표와 충돌하지 않는지 검증
3. **영향 분석**: 개정으로 인해 영향받는 템플릿 및 문서 목록 작성
4. **승인**: 프로젝트 리더 또는 핵심 기여자의 승인
5. **동기화**: 모든 종속 문서 및 템플릿 업데이트
6. **버전 갱신**: 시맨틱 버저닝 규칙에 따라 버전 증가

### 버전 관리 규칙

- **MAJOR (X.0.0)**: 기존 원칙의 제거 또는 근본적 재정의
- **MINOR (0.X.0)**: 새로운 원칙 추가 또는 기존 원칙의 실질적 확장
- **PATCH (0.0.X)**: 명확화, 문구 수정, 오타 수정 등 비의미론적 개선

### 준수 검증

모든 Pull Request는 다음을 검증해야 합니다:

- **원칙 I 준수**: 모든 문서가 한국어로 작성되었는가?
- **원칙 II 준수**: C#/.NET 10 외 다른 백엔드 기술이 사용되지 않았는가?
- **템플릿 준수**: 해당되는 템플릿 구조를 따랐는가?
- **문서 완결성**: 모든 필수 섹션이 작성되었는가?

### 예외 처리

원칙 위반이 불가피한 경우:

1. **문서화**: 위반 사항과 사유를 명확히 기록
2. **정당화**: 대안이 불가능한 이유 설명
3. **승인**: 명시적 승인 필요
4. **제한**: 예외는 최소 범위로 한정

복잡성이나 예외 사항은 plan.md의 "Complexity Tracking" 섹션에 문서화해야 합니다.

### 런타임 가이드

개발 중 실시간 참조가 필요한 경우, `.specify/templates/agent-file-template.md`에서 생성된 종합 가이드 문서를 참조하십시오.

---

**버전 (Version)**: 1.0.0 | **비준일 (Ratified)**: 2025-11-17 | **최종 개정일 (Last Amended)**: 2025-11-17
