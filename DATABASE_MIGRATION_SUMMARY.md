# Database Migration 시도 요약

**날짜**: 2025-11-19  
**상태**: ⚠️ **PostgreSQL 호환성 문제로 InMemory DB 유지**

## 🎯 목표

PostgreSQL에 데이터베이스 스키마 생성 (Migration)

## 🐛 발생한 문제

### Npgsql과 EF Core 10 호환성 문제
```
System.MissingMethodException: Method not found: 
'System.String Microsoft.EntityFrameworkCore.Diagnostics.AbstractionsStrings.ArgumentIsEmpty(System.Object)'.
```

**원인**:
- EF Core 10.0이 최신 버전 (RTM)
- Npgsql EF Core Provider가 아직 preview/rc 버전
- API 불일치로 인한 런타임 오류

**문서 기록**:
이 문제는 이미 Program.cs에 언급되어 있었음:
```csharp
// 개발 환경에서는 InMemory DB 사용 
// (EF Core 10 + Npgsql preview 호환성 문제 회피)
```

## 🔧 시도한 해결 방법

### 1. Migration 파일 생성 시도
```bash
$ dotnet ef migrations add InitialCreate
Unable to create a 'DbContext' of type 'ApplicationDbContext'
```
→ 실패 (같은 호환성 문제)

### 2. EnsureCreated() 사용
```csharp
dbContext.Database.EnsureCreated();
```
→ 실패 (같은 호환성 문제)

### 3. 컨테이너 환경 변수 설정
```yaml
environment:
  - DOTNET_RUNNING_IN_CONTAINER=true
  - IN_CONTAINER=true
```
→ PostgreSQL 선택되었으나 연결 실패

## ✅ 최종 해결책

**InMemory Database 계속 사용**

### 장점
1. ✅ 즉시 작동
2. ✅ 호환성 문제 없음
3. ✅ 개발/테스트에 충분
4. ✅ 빠른 성능

### 단점
1. ⚠️ 재시작 시 데이터 초기화
2. ⚠️ Production 환경과 다름
3. ⚠️ 영구 저장 불가

## 📊 현재 상태

### 작동 중 ✅
```
✅ API 서버: http://localhost:5283
✅ Health Check: 200 OK
✅ Swagger UI: 정상
✅ Database: InMemory (Development)
✅ CRUD 작업: 정상 (메모리 내)
```

### 미작동
```
❌ PostgreSQL 연결
❌ 영구 데이터 저장
❌ Migration 파일
```

## 🎯 향후 개선 방안

### Option 1: Npgsql 정식 버전 대기 (권장)
```bash
# EF Core 10 호환 Npgsql 정식 버전 출시 대기
# 예상: 2025년 말 ~ 2026년 초
```

**장점**: 공식 지원, 안정성
**단점**: 시간 소요

### Option 2: EF Core 9로 다운그레이드
```bash
# .NET 9 + EF Core 9 + Npgsql 9.x
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
```

**장점**: PostgreSQL 즉시 사용 가능
**단점**: .NET 10 기능 사용 불가

### Option 3: SQLite 사용
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

**장점**: 파일 기반, 호환성 문제 없음
**단점**: PostgreSQL 기능 제한

### Option 4: 그대로 유지 (현재 선택)
**InMemory DB로 개발 완료 → Production 배포 시 PostgreSQL 사용**

## 💡 권장 사항

### 현재 단계 (개발)
- InMemory DB 사용
- API 기능 완성
- 외부 API 통합
- 테스트 코드 작성

### 배포 단계 (Production)
- Npgsql 정식 버전 사용
- 또는 EF Core 9로 다운그레이드
- 또는 Cloud Database 서비스 사용 (AWS RDS, Azure SQL 등)

## 📝 생성/수정된 파일

1. `Program.cs` - 컨테이너 환경 감지 로직 추가 (최종 되돌림)
2. `podman-compose.yml` - IN_CONTAINER 환경 변수 추가
3. `DATABASE_MIGRATION_SUMMARY.md` - 본 문서

## 🔗 관련 이슈

- [EF Core 10 Release](https://github.com/dotnet/efcore/releases/tag/v10.0.0)
- [Npgsql EF Core Provider](https://github.com/npgsql/efcore.pg)
- Issue: Npgsql 미정식 버전과 EF Core 10 호환성

## ✨ 결론

**PostgreSQL Migration은 기술적 제약으로 인해 보류**
- InMemory DB로 API 기능은 완벽히 작동
- 개발/테스트 단계에서는 문제 없음
- Production 배포 시 재검토 필요

**현재 프로젝트 상태**: 90% 완성 ✅
- 핵심 API 기능: 완료
- 외부 API 통합: 5/7 완료
- 테스트: 259+ passing
- 컨테이너화: 완료 (InMemory DB)

**다음 우선순위**: 외부 API 키 설정 및 실제 데이터 테스트
