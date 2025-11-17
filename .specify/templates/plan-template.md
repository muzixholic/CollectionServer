# 구현 계획 (Implementation Plan): [FEATURE]

**브랜치 (Branch)**: `[###-feature-name]` | **날짜 (Date)**: [DATE] | **사양서 (Spec)**: [link]
**입력 (Input)**: Feature specification from `/specs/[###-feature-name]/spec.md`

**참고 (Note)**: 이 템플릿은 `/speckit.plan` 명령으로 작성됩니다. 실행 워크플로우는 `.specify/templates/commands/plan.md`를 참조하세요.

## 요약 (Summary)

[Extract from feature spec: primary requirement + technical approach from research]

## 기술 컨텍스트 (Technical Context)

<!--
  작업 필요 (ACTION REQUIRED): 이 섹션의 내용을 프로젝트의 기술적 세부사항으로 
  교체하세요. 여기 제시된 구조는 반복 프로세스를 안내하기 위한 권고사항입니다.
-->

**언어/버전 (Language/Version)**: C# / .NET 10 (헌장 필수)  
**주요 의존성 (Primary Dependencies)**: [e.g., ASP.NET Core, Entity Framework Core, Dapper or NEEDS CLARIFICATION]  
**저장소 (Storage)**: [if applicable, e.g., SQL Server, PostgreSQL, MongoDB, files or N/A]  
**테스트 (Testing)**: [e.g., xUnit, NUnit, MSTest or NEEDS CLARIFICATION]  
**대상 플랫폼 (Target Platform)**: [e.g., Linux server, Windows Server, Docker, Kubernetes or NEEDS CLARIFICATION]
**프로젝트 유형 (Project Type)**: [single/web/mobile - determines source structure]  
**성능 목표 (Performance Goals)**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**제약사항 (Constraints)**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**규모/범위 (Scale/Scope)**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## 헌장 준수 검증 (Constitution Check)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 필수 검증 항목

- [ ] **한국어 필수 (Principle I)**: 모든 문서가 한국어로 작성되는가?
  - spec.md, plan.md, research.md, data-model.md, quickstart.md, contracts/
  - 코드 주석 (복잡한 로직)
  - API 문서 및 에러 메시지
  
- [ ] **C#/.NET 10 스택 (Principle II)**: 기술 스택이 헌장을 준수하는가?
  - 백엔드 언어: C# 전용
  - 프레임워크: .NET 10
  - 다른 백엔드 언어(Python, Java, Node.js 등) 미사용
  
- [ ] **문서 완결성**: 모든 필수 섹션이 작성되었는가?

[추가 검증 항목은 헌장 파일 기반으로 결정]

## 프로젝트 구조 (Project Structure)

### 문서 (이 기능용)

```text
specs/[###-feature]/
├── plan.md              # 이 파일 (/speckit.plan 명령 출력)
├── research.md          # Phase 0 출력 (/speckit.plan 명령)
├── data-model.md        # Phase 1 출력 (/speckit.plan 명령)
├── quickstart.md        # Phase 1 출력 (/speckit.plan 명령)
├── contracts/           # Phase 1 출력 (/speckit.plan 명령)
└── tasks.md             # Phase 2 출력 (/speckit.tasks 명령 - /speckit.plan으로 생성 안됨)
```

### 소스 코드 (Source Code - repository root)
<!--
  작업 필요 (ACTION REQUIRED): 아래 플레이스홀더 트리를 이 기능의 구체적인 레이아웃으로
  교체하세요. 사용하지 않는 옵션은 삭제하고 선택한 구조를 실제 경로로 확장하세요
  (예: apps/admin, packages/something). 제공된 계획에 Option 레이블이 포함되지 않아야 합니다.
-->

```text
# [미사용시 삭제] 옵션 1: 단일 프로젝트 (기본값)
src/
├── Models/
├── Services/
├── Controllers/ 또는 Endpoints/
└── Infrastructure/

tests/
├── ContractTests/
├── IntegrationTests/
└── UnitTests/

# [미사용시 삭제] 옵션 2: 웹 애플리케이션 ("frontend" + "backend" 감지 시)
backend/
├── src/
│   ├── Models/
│   ├── Services/
│   └── Api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [미사용시 삭제] 옵션 3: 모바일 + API ("iOS/Android" 감지 시)
api/
└── [backend와 동일 구조]

ios/ 또는 android/
└── [플랫폼별 구조: 기능 모듈, UI 플로우, 플랫폼 테스트]
```

**구조 결정 (Structure Decision)**: [선택된 구조를 문서화하고 위에 기록된 실제 디렉토리를 참조]

## 복잡성 추적 (Complexity Tracking)

> **헌장 준수 검증에서 위반사항이 있고 정당화가 필요한 경우에만 작성**

| 위반사항 (Violation) | 필요한 이유 (Why Needed) | 거부된 간단한 대안 (Simpler Alternative Rejected Because) |
|-----------|------------|-------------------------------------|
| [e.g., 4번째 프로젝트] | [현재 필요사항] | [3개 프로젝트로 불충분한 이유] |
| [e.g., Repository 패턴] | [특정 문제] | [직접 DB 접근이 불충분한 이유] |
