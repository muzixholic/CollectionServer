# API 키 설정 상태 및 디버깅 가이드

**날짜**: 2025-11-19  
**상태**: ⚠️ **진행 중** (Provider 호출 문제)

## ✅ 완료된 설정

### 1. User Secrets 구성
```bash
# UserSecretsId 추가됨
✅ CollectionServer.Api.csproj에 UserSecretsId 추가
✅ Kakao API 키 저장 완료

# 확인
$ cd src/CollectionServer.Api
$ dotnet user-secrets list
ExternalApis:KakaoBook:ApiKey = f661a532addc0622d536fb30f4c74022
```

### 2. KakaoBookProvider 완전 구현
```csharp
✅ GetMediaByBarcodeAsync() 구현 완료
✅ Kakao Book Search API 연동
✅ JSON 역직렬화
✅ Book 엔티티 매핑
```

### 3. Provider 등록
```csharp
✅ ServiceCollectionExtensions.cs에 등록
✅ AddExternalApiSettings() 호출 확인
```

### 4. 컨테이너 환경 변수
```yaml
✅ podman-compose.yml에 API 키 추가
environment:
  - ExternalApis__KakaoBook__ApiKey=f661a532addc0622d536fb30f4c74022
```

### 5. API 키 검증
```bash
✅ Kakao API 직접 호출 성공
$ curl -H "Authorization: KakaoAK KEY" "https://dapi.kakao.com/v3/search/book?query=9788966262281&target=isbn"
→ 데이터 정상 반환 (이펙티브 자바)
```

## ⚠️ 미해결 문제

### 증상
```bash
$ curl http://localhost:5283/items/9788966262281
{
  "statusCode": 404,
  "message": "미디어 정보를 찾을 수 없습니다."
}
```

### 분석
1. ✅ API 서버 실행 중
2. ✅ Health Check 정상
3. ✅ Kakao API 직접 호출 성공
4. ✅ SupportsBarcode() 로직 정상
5. ❌ Provider가 호출되지 않음 (로그 없음)

## 🔍 디버깅 체크리스트

### 1. Provider 등록 확인
```bash
# Program.cs에서 AddExternalApiSettings 호출 확인
✅ builder.Services.AddExternalApiSettings(builder.Configuration);

# ServiceCollectionExtensions.cs 확인
✅ services.AddScoped<IMediaProvider, KakaoBookProvider>();
```

### 2. SupportsBarcode 로직
```python
# 테스트 완료 - 정상
barcode = "9788966262281"
cleaned = "9788966262281"
length = 13
starts_with_978 = True
result = True ✅
```

### 3. ExternalApiSettings 바인딩
```bash
# 확인 필요
- appsettings.json의 ExternalApis 섹션
- 환경 변수 바인딩
- Options 패턴 작동
```

### 4. MediaService 로깅
```csharp
// 추가 필요
_logger.LogInformation("Found {Count} providers", _providers.Count());
_logger.LogInformation("Supported providers: {Count}", supportedProviders.Count);

foreach (var provider in supportedProviders)
{
    _logger.LogInformation("Trying provider: {Provider}", provider.ProviderName);
}
```

## 🎯 다음 세션 TODO

### 즉시 확인
1. ☐ appsettings.json에 ExternalApis 섹션 확인
2. ☐ Provider 로깅 추가
3. ☐ MediaService에서 _providers.Count() 출력
4. ☐ SupportsBarcode() 호출 여부 로깅

### 디버깅 코드 추가
```csharp
// MediaService.cs GetMediaByBarcodeAsync()
_logger.LogInformation("Total providers registered: {Count}", _providers.Count());

var supportedProviders = _providers
    .Where(p => {
        var supports = p.SupportsBarcode(barcode);
        _logger.LogInformation("Provider {Name} supports {Barcode}: {Supports}", 
            p.ProviderName, barcode, supports);
        return supports;
    })
    .OrderBy(p => p.Priority)
    .ToList();

_logger.LogInformation("Supported providers count: {Count}", supportedProviders.Count);
```

### 가능한 해결책

#### Option 1: appsettings.json 확인
```json
// src/CollectionServer.Api/appsettings.json 또는 appsettings.Development.json
{
  "ExternalApis": {
    "KakaoBook": {
      "BaseUrl": "https://dapi.kakao.com",
      "Priority": 2,
      "TimeoutSeconds": 10
    }
  }
}
```

#### Option 2: 환경 변수 형식 확인
```bash
# 현재: ExternalApis__KakaoBook__ApiKey
# 필요할 수도: ExternalApis:KakaoBook:ApiKey (설정 파일에서)
```

#### Option 3: HttpClientFactory 설정
```csharp
// KakaoBookProvider 생성자에 로깅 추가
_logger.LogInformation("KakaoBookProvider created with API Key: {HasKey}", 
    !string.IsNullOrEmpty(_settings.ApiKey));
```

## 📝 검증 테스트

### 로컬 환경
```bash
cd src/CollectionServer.Api
dotnet run
# 다른 터미널에서
curl http://localhost:5283/items/9788966262281
```

### 컨테이너 환경
```bash
podman-compose up -d
curl http://localhost:5283/items/9788966262281
podman logs collectionserver-api | grep -i "provider\|kakao"
```

### 직접 Provider 테스트
```bash
# Unit Test 추가
[Fact]
public async Task KakaoBookProvider_ShouldReturnBook_WhenValidISBN()
{
    // Arrange
    var provider = new KakaoBookProvider(...);
    
    // Act
    var result = await provider.GetMediaByBarcodeAsync("9788966262281");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("이펙티브 자바", result.Title);
}
```

## 📊 현재 상태

### 작동 중
- ✅ API 서버
- ✅ Health Check
- ✅ Swagger UI
- ✅ InMemory Database
- ✅ Kakao API (직접 호출)

### 미작동
- ❌ Provider 호출
- ❌ 외부 API 통합

### 설정 완료
- ✅ API 키 (User Secrets)
- ✅ Provider 구현
- ✅ Provider 등록
- ✅ 컨테이너 환경 변수

## 🔗 관련 파일

1. `src/CollectionServer.Api/Program.cs` - 서비스 등록
2. `src/CollectionServer.Api/Extensions/ServiceCollectionExtensions.cs` - Provider 등록
3. `src/CollectionServer.Infrastructure/ExternalApis/Books/KakaoBookProvider.cs` - 구현
4. `src/CollectionServer.Core/Services/MediaService.cs` - Provider 사용
5. `src/CollectionServer.Api/appsettings.json` - 설정
6. `podman-compose.yml` - 컨테이너 환경 변수

## 💡 힌트

Provider가 호출되지 않는 경우, 가장 가능성 높은 원인:
1. **appsettings.json에 ExternalApis 섹션 없음**
2. Options 패턴 바인딩 실패
3. Provider 생성자에서 예외 발생

---

**다음 세션 시작 지점**: MediaService에 로깅 추가하여 Provider 등록 확인
