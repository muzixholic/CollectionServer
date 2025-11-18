# API 사용 가이드

이 문서는 CollectionServer API를 사용하는 방법을 설명합니다.

## 목차
- [기본 사항](#기본-사항)
- [인증](#인증)
- [엔드포인트](#엔드포인트)
- [오류 처리](#오류-처리)
- [Rate Limiting](#rate-limiting)
- [예제](#예제)

## 기본 사항

### Base URL
- **개발**: `http://localhost:5000`
- **프로덕션**: `https://your-domain.com`

### Content-Type
모든 요청과 응답은 `application/json` 형식을 사용합니다.

### API 버전
현재 버전: `v1`

## 인증

현재 버전에서는 인증이 필요하지 않습니다. 단, Rate Limiting이 적용됩니다.

## 엔드포인트

### 1. 미디어 정보 조회

바코드로 미디어 정보를 조회합니다.

```http
GET /items/{barcode}
```

#### 파라미터

| 이름 | 타입 | 필수 | 설명 |
|------|------|------|------|
| barcode | string | ✅ | ISBN-10, ISBN-13, UPC, EAN-13 바코드 |

#### 응답

**성공 (200 OK)**

도서 예시:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "barcode": "9788950991234",
  "barcodeType": "ISBN13",
  "mediaType": "Book",
  "title": "클린 코드",
  "description": "애자일 소프트웨어 장인 정신",
  "imageUrl": "https://example.com/image.jpg",
  "source": "GoogleBooks",
  "authors": ["로버트 C. 마틴"],
  "publisher": "인사이트",
  "publishedDate": "2013-12-24T00:00:00",
  "isbn": "9788950991234",
  "pageCount": 584,
  "language": "ko",
  "createdAt": "2025-11-18T10:00:00Z",
  "updatedAt": "2025-11-18T10:00:00Z"
}
```

영화 예시:
```json
{
  "id": "uuid",
  "barcode": "123456789012",
  "barcodeType": "UPC",
  "mediaType": "Movie",
  "title": "인셉션",
  "description": "꿈 속의 꿈...",
  "imageUrl": "https://...",
  "source": "TMDb",
  "director": "크리스토퍼 놀란",
  "actors": ["레오나르도 디카프리오", "조셉 고든 레빗"],
  "releaseDate": "2010-07-21T00:00:00",
  "runtime": 148,
  "genre": "SF",
  "rating": "PG-13"
}
```

음악 앨범 예시:
```json
{
  "id": "uuid",
  "barcode": "123456789012",
  "barcodeType": "EAN13",
  "mediaType": "MusicAlbum",
  "title": "Dark Side of the Moon",
  "description": "Progressive rock masterpiece",
  "imageUrl": "https://...",
  "source": "MusicBrainz",
  "artist": "Pink Floyd",
  "releaseDate": "1973-03-01T00:00:00",
  "genre": "Progressive Rock",
  "label": "Harvest Records",
  "tracks": [
    { "trackNumber": 1, "title": "Speak to Me", "duration": 90 },
    { "trackNumber": 2, "title": "Breathe", "duration": 163 }
  ]
}
```

### 2. 헬스 체크

API 서버 상태를 확인합니다.

```http
GET /health
```

#### 응답

```json
{
  "status": "healthy",
  "timestamp": "2025-11-18T10:00:00Z"
}
```

## 오류 처리

모든 오류 응답은 다음 형식을 따릅니다:

```json
{
  "statusCode": 400,
  "message": "잘못된 바코드 형식입니다.",
  "details": "바코드는 10자리 또는 13자리여야 합니다. 올바른 형식: ISBN-10 (10자리), ISBN-13 (13자리), UPC (12자리), EAN-13 (13자리)",
  "timestamp": "2025-11-18T10:00:00Z",
  "traceId": "00-abc123..."
}
```

### 오류 코드

| 상태 코드 | 메시지 | 설명 |
|-----------|--------|------|
| 400 | 잘못된 바코드 형식입니다 | 바코드가 유효하지 않음 |
| 404 | 미디어 정보를 찾을 수 없습니다 | 모든 소스에서 정보를 찾을 수 없음 |
| 429 | 요청 제한을 초과했습니다 | Rate Limit 초과 |
| 500 | 서버 내부 오류가 발생했습니다 | 서버 오류 |
| 503 | 서비스를 사용할 수 없습니다 | 데이터베이스 연결 실패 등 |

## Rate Limiting

### 제한 사항
- **개발**: 100 요청/분
- **프로덕션**: 200 요청/분

### 초과 시
HTTP 429 응답과 함께 `Retry-After` 헤더가 반환됩니다:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json

{
  "statusCode": 429,
  "message": "요청 제한을 초과했습니다. 잠시 후 다시 시도해주세요.",
  "details": "1분당 100개 요청만 가능합니다."
}
```

## 예제

### cURL

```bash
# 도서 조회
curl -X GET "http://localhost:5000/items/9788950991234" \
  -H "Accept: application/json"

# 헬스 체크
curl -X GET "http://localhost:5000/health"
```

### C# (.NET)

```csharp
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5000") 
};

// 미디어 조회
var response = await client.GetAsync("/items/9788950991234");
if (response.IsSuccessStatusCode)
{
    var json = await response.Content.ReadAsStringAsync();
    var media = JsonSerializer.Deserialize<MediaItem>(json);
    Console.WriteLine($"Title: {media.Title}");
}
```

### JavaScript (Fetch API)

```javascript
// 미디어 조회
fetch('http://localhost:5000/items/9788950991234')
  .then(response => {
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return response.json();
  })
  .then(data => {
    console.log('Title:', data.title);
    console.log('Media Type:', data.mediaType);
  })
  .catch(error => {
    console.error('Error:', error);
  });
```

### Python (requests)

```python
import requests

# 미디어 조회
response = requests.get('http://localhost:5000/items/9788950991234')

if response.status_code == 200:
    data = response.json()
    print(f"Title: {data['title']}")
    print(f"Media Type: {data['mediaType']}")
elif response.status_code == 404:
    print("미디어를 찾을 수 없습니다")
elif response.status_code == 429:
    retry_after = response.headers.get('Retry-After')
    print(f"Rate limit 초과. {retry_after}초 후 재시도하세요")
```

## 데이터 소스 우선순위

### 도서 (Books)
1. Database (캐시)
2. Google Books API
3. Kakao Book API
4. Aladin API

### 영화 (Movies)
1. Database (캐시)
2. TMDb API
3. OMDb API

### 음악 (Music Albums)
1. Database (캐시)
2. MusicBrainz API
3. Discogs API

## Database-First 아키텍처

1. **첫 번째 조회**: 데이터베이스 확인 (<500ms)
2. **캐시 미스**: 외부 API 호출 (<2초)
3. **데이터 저장**: 외부 API 결과를 데이터베이스에 캐싱
4. **후속 조회**: 데이터베이스에서 빠르게 반환

이를 통해 외부 API 의존성을 줄이고 응답 시간을 최적화합니다.

## 지원 및 문의

- GitHub Issues: [링크]
- 이메일: contact@example.com
- 문서: `/swagger` 엔드포인트
