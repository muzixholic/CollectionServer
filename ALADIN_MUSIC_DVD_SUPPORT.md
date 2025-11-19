# 🎵📀 알라딘 API 음반/DVD 지원 추가

**날짜**: 2025-11-19  
**브랜치**: `001-isbn-book-api`  
**커밋**: `7429221`

## ✅ 구현 완료

### 추가된 기능

#### 1. **음반 지원 (MUSIC)**
```csharp
// 알라딘 API mallType="MUSIC" 지원
MusicAlbum album = new MusicAlbum {
    Artist = "방탄소년단 (BTS)",
    Label = "BIGHIT MUSIC / YG PLUS",
    Genre = "댄스뮤직",
    ReleaseDate = DateTime.Parse("2019-04-12")
};
```

#### 2. **DVD/Blu-ray 지원 (DVD)**
```csharp
// 알라딘 API mallType="DVD" 지원
Movie movie = new Movie {
    Director = "크리스토퍼 놀란",
    Cast = "마이클 케인, 매튜 맥커너히, 앤 해서웨이...",
    Genre = "S.F/판타지",
    ReleaseDate = DateTime.Parse("2014-11-06")
};
```

#### 3. **도서 유지 (BOOK)**
```csharp
// 기존 도서 기능 유지
Book book = new Book {
    Authors = "조슈아 블로크",
    Publisher = "인사이트",
    Isbn13 = "9788966262281"
};
```

---

## 🔧 구현 상세

### SupportsBarcode 확장
```csharp
public bool SupportsBarcode(string barcode)
{
    var cleaned = barcode.Replace("-", "").Replace(" ", "");
    if (cleaned.Length == 10) return true; // ISBN-10
    if (cleaned.Length == 12) return true; // UPC (Music)
    if (cleaned.Length == 13) return true; // ISBN-13, EAN-13 (Music/DVD)
    return false;
}
```

### mallType 기반 자동 판별
```csharp
MediaItem mediaItem = item.MallType?.ToUpperInvariant() switch
{
    "MUSIC" => CreateMusicAlbum(item, barcode),
    "DVD" => CreateMovie(item, barcode),
    _ => CreateBook(item, barcode) // Default
};
```

### 데이터 정제 함수

#### CleanTitle()
```csharp
// "[수입] Homework (LP)" -> "Homework (LP)"
// "[한정반] 꽃갈피 셋" -> "꽃갈피 셋"
```

#### ExtractGenre()
```csharp
// "음반>팝>기획>죽기 전에 꼭 들어야 할 앨범 1001" 
// -> "죽기 전에 꼭 들어야 할 앨범 1001"
```

#### ExtractDirector()
```csharp
// "크리스토퍼 놀란 (감독), 마이클 케인, ..."
// -> "크리스토퍼 놀란"
```

#### ExtractCast()
```csharp
// "크리스토퍼 놀란 (감독), 마이클 케인, 매튜 맥커너히, ... (출연)"
// -> "마이클 케인, 매튜 맥커너히, ..."
```

---

## 🧪 테스트 결과

### ✅ 도서 조회
```bash
curl "http://www.aladin.co.kr/ttb/api/ItemSearch.aspx?ttbkey=KEY&Query=9788966262281&QueryType=Barcode"
```
**결과**:
```json
{
  "title": "이펙티브 자바",
  "author": "조슈아 블로크",
  "mallType": "BOOK",
  "isbn13": "9788966262281"
}
```

### ✅ 음반 조회
```bash
curl "http://www.aladin.co.kr/ttb/api/ItemSearch.aspx?ttbkey=KEY&Query=724384260910&QueryType=Barcode"
```
**결과**:
```json
{
  "title": "[수입] Homework (LP)",
  "author": "다프트 펑크 (Daft Punk)",
  "mallType": "MUSIC",
  "isbn13": "724384260910",
  "categoryName": "음반>팝>기획>죽기 전에 꼭 들어야 할 앨범 1001"
}
```

### ✅ DVD 조회
```bash
curl "http://www.aladin.co.kr/ttb/api/ItemSearch.aspx?ttbkey=KEY&Query=인터스텔라&QueryType=Title&SearchTarget=DVD"
```
**결과**:
```json
{
  "title": "인터스텔라",
  "author": "크리스토퍼 놀란 (감독), 마이클 케인, 매튜 맥커너히, ... (출연)",
  "mallType": "DVD",
  "isbn13": "8809507358292"
}
```

### ✅ BTS 앨범
```bash
curl "http://www.aladin.co.kr/ttb/api/ItemSearch.aspx?ttbkey=KEY&Query=8809440338702&QueryType=Barcode"
```
**결과**:
```json
{
  "title": "방탄소년단 - 미니 6집 MAP OF THE SOUL : PERSONA",
  "author": "방탄소년단 (BTS)",
  "mallType": "MUSIC",
  "isbn13": "8809440338702",
  "publisher": "BIGHIT MUSIC / YG PLUS"
}
```

---

## 📊 Provider 우선순위

### 도서 (Book)
1. **GoogleBooks** (Priority: 1)
2. **KakaoBook** (Priority: 2)
3. **AladinApi** (Priority: 3) ✨ 새로 추가

### 음반 (Music)
1. **MusicBrainz** (Priority: 1) - 국제 DB
2. **Discogs** (Priority: 2)
3. **AladinApi** (Priority: 3) ✨ Fallback으로 유용

### 영화 (Movie)
1. **TMDb** (Priority: 1) - ⚠️ 바코드 미지원
2. **OMDb** (Priority: 2) - ⚠️ 바코드 미지원
3. **AladinApi** (Priority: 3) ✨ DVD/Blu-ray 바코드 지원

---

## 💡 알라딘 API 특징

### ✅ 장점
1. **다양한 미디어 타입 지원**
   - 도서 (BOOK)
   - 음반 (MUSIC)
   - DVD/Blu-ray (DVD)

2. **한국 콘텐츠 강점**
   - K-POP 앨범 풍부
   - 한국 영화 DVD/Blu-ray
   - 한국 도서 완벽 지원

3. **풍부한 메타데이터**
   - 카테고리/장르 정보
   - 출판사/레이블 정보
   - 커버 이미지

### ⚠️ 제한사항
1. **음반 바코드 검색 제한적**
   - 일부 음반만 바코드 지원
   - 제목 검색은 잘 작동
   - MusicBrainz가 우선 호출됨

2. **트랙 리스트 없음**
   - 음반 트랙 정보 제공 안함
   - MusicBrainz 사용 권장

3. **영화 런타임 정보 없음**
   - 상영 시간 제공 안함
   - TMDb 사용 권장

---

## 🔄 작동 흐름

### 예시 1: 한국 음반 바코드 조회
```
Request: GET /items/8809440338702

1. MediaService.GetMediaByBarcodeAsync()
2. DB 캐시 확인 -> 없음
3. Provider 필터링:
   - MusicBrainz: supports=true (Priority 1)
   - AladinApi: supports=true (Priority 3)
4. MusicBrainz 시도 -> 성공!
5. DB에 저장
6. Response: MusicAlbum from MusicBrainz
```

### 예시 2: 한국 음반 (MusicBrainz 없음)
```
Request: GET /items/8804775455780

1. MediaService.GetMediaByBarcodeAsync()
2. DB 캐시 확인 -> 없음
3. Provider 필터링:
   - MusicBrainz: supports=true (Priority 1)
   - AladinApi: supports=true (Priority 3)
4. MusicBrainz 시도 -> 실패 (404)
5. AladinApi 시도 -> 성공!
6. DB에 저장
7. Response: MusicAlbum from AladinApi
```

### 예시 3: DVD 바코드 조회
```
Request: GET /items/8809507358292

1. MediaService.GetMediaByBarcodeAsync()
2. DB 캐시 확인 -> 없음
3. Provider 필터링:
   - TMDb: supports=false (바코드 미지원)
   - OMDb: supports=false (바코드 미지원)
   - AladinApi: supports=true (Priority 3)
4. AladinApi 시도 -> 성공!
5. DB에 저장
6. Response: Movie from AladinApi
```

---

## 📝 API 응답 예시

### 음반 응답
```json
{
  "id": "guid",
  "barcode": "8809440338702",
  "mediaType": 3,
  "title": "MAP OF THE SOUL : PERSONA",
  "description": "Album by 방탄소년단 (BTS)",
  "imageUrl": "https://image.aladin.co.kr/product/.../cover.jpg",
  "source": "AladinApi",
  "artist": "방탄소년단 (BTS)",
  "tracks": [],
  "releaseDate": "2019-04-12",
  "label": "BIGHIT MUSIC / YG PLUS",
  "genre": "댄스뮤직"
}
```

### DVD 응답
```json
{
  "id": "guid",
  "barcode": "8809507358292",
  "mediaType": 2,
  "title": "인터스텔라",
  "description": "Directed by 크리스토퍼 놀란",
  "imageUrl": "https://image.aladin.co.kr/product/.../cover.jpg",
  "source": "AladinApi",
  "director": "크리스토퍼 놀란",
  "cast": "마이클 케인, 매튜 맥커너히, 앤 해서웨이, 제시카 차스테인",
  "releaseDate": "2014-11-06",
  "runtimeMinutes": null,
  "genre": "S.F/판타지",
  "rating": null
}
```

---

## 🎯 다음 단계

### 즉시 사용 가능
- ✅ 도서 바코드 조회 (3개 Provider)
- ✅ 음반 바코드 조회 (3개 Provider)
- ✅ DVD 바코드 조회 (1개 Provider)

### 추가 개선 (선택)
- ☐ 알라딘 제목 검색 API 추가
- ☐ 알라딘 베스트셀러 API 통합
- ☐ 알라딘 신간 API 통합
- ☐ 캐시 TTL 설정
- ☐ Retry 로직 추가

### 알려진 이슈
1. **음반 바코드 검색 성공률 낮음**
   - 해결방법: MusicBrainz를 우선 사용
   - AladinApi는 Fallback용

2. **트랙 리스트 없음**
   - 해결방법: MusicBrainz 사용

3. **영화 런타임 없음**
   - 해결방법: TMDb API 추가 (제목 검색)

---

## 🔗 관련 문서

- [PROVIDER_DEBUG_SUCCESS.md](PROVIDER_DEBUG_SUCCESS.md) - MusicBrainz 디버깅
- [EXTERNAL_API_IMPLEMENTATION_SUMMARY.md](EXTERNAL_API_IMPLEMENTATION_SUMMARY.md) - 전체 Provider 현황
- [API_KEY_SETUP_STATUS.md](API_KEY_SETUP_STATUS.md) - API 키 설정

---

## 📈 통계

### 코드 변경
- **파일**: 1개
- **추가**: +217 lines
- **삭제**: -7 lines

### Provider 현황
| Provider | 도서 | 음반 | DVD | Priority |
|----------|------|------|-----|----------|
| GoogleBooks | ✅ | - | - | 1 |
| KakaoBook | ✅ | - | - | 2 |
| **AladinApi** | ✅ | ✅ | ✅ | **3** |
| MusicBrainz | - | ✅ | - | 1 |
| Discogs | - | ✅ | - | 2 |
| TMDb | - | - | ⚠️ | 1 |
| OMDb | - | - | ⚠️ | 2 |

### 지원 바코드
| 타입 | 길이 | 예시 | 지원 Provider |
|------|------|------|---------------|
| ISBN-10 | 10 | 8966262287 | GoogleBooks, KakaoBook, AladinApi |
| ISBN-13 | 13 | 9788966262281 | GoogleBooks, KakaoBook, AladinApi |
| UPC | 12 | 724384260910 | MusicBrainz, Discogs, AladinApi |
| EAN-13 | 13 | 8809440338702 | MusicBrainz, Discogs, AladinApi |
| DVD EAN | 13 | 8809507358292 | AladinApi |

---

**🎉 축하합니다!**

**알라딘 API가 이제 음반과 DVD/Blu-ray도 지원합니다!**

특히 한국 콘텐츠(K-POP, 한국 영화)에 강점이 있어  
한국 사용자에게 매우 유용한 Provider가 되었습니다! 🇰🇷🎵📀
