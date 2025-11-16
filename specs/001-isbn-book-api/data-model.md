# Data Model: 미디어 정보 API 서버

**Feature**: 001-isbn-book-api  
**Phase**: 1 - Data Model Design  
**Date**: 2025-11-16

## 개요

Database-First 아키텍처를 지원하기 위한 통합 미디어 데이터 모델을 정의합니다. PostgreSQL 데이터베이스와 Entity Framework Core 10.0을 사용하여 도서, 영화, 음악 앨범의 공통 필드와 특화 필드를 관리합니다.

## 엔티티 다이어그램

```text
┌─────────────────────────────────────────────────────────────┐
│                        MediaItem (Base)                      │
├─────────────────────────────────────────────────────────────┤
│ + Id: Guid (PK)                                             │
│ + Barcode: string (UNIQUE, INDEX)                           │
│ + MediaType: MediaType (enum)                               │
│ + Title: string                                             │
│ + Description: string?                                      │
│ + ImageUrl: string?                                         │
│ + Source: string (API source name)                          │
│ + CreatedAt: DateTime                                       │
│ + UpdatedAt: DateTime                                       │
└─────────────────────────────────────────────────────────────┘
                              △
                              │ (TPT Inheritance)
       ┌──────────────────────┼──────────────────────┐
       │                      │                      │
┌──────┴──────┐      ┌────────┴────────┐     ┌──────┴──────┐
│    Book     │      │     Movie       │     │ MusicAlbum  │
├─────────────┤      ├─────────────────┤     ├─────────────┤
│ Isbn13      │      │ Director        │     │ Artist      │
│ Authors[]   │      │ Cast[]          │     │ Tracks[]    │
│ Publisher   │      │ RuntimeMinutes  │     │ ReleaseDate │
│ PublishDate │      │ ReleaseDate     │     │ Label       │
│ PageCount   │      │ Rating          │     │ Genre       │
│ Genre       │      │ Genre           │     └─────────────┘
└─────────────┘      └─────────────────┘
```

## 엔티티 정의

### 1. MediaItem (기본 엔티티)

**용도**: 모든 미디어 타입의 공통 속성을 정의하는 추상 기본 클래스

**필드**:

| 필드명 | 타입 | 제약 조건 | 설명 |
|--------|------|----------|------|
| Id | Guid | PK, NOT NULL | 기본 키 (UUID) |
| Barcode | string | UNIQUE, NOT NULL, INDEX, MaxLength(13) | ISBN-10/13, UPC, EAN-13 |
| MediaType | MediaType | NOT NULL | Book, Movie, MusicAlbum |
| Title | string | NOT NULL, MaxLength(500) | 미디어 제목 |
| Description | string? | NULL, MaxLength(5000) | 설명/줄거리/소개 |
| ImageUrl | string? | NULL, MaxLength(2000) | 표지/커버 이미지 URL |
| Source | string | NOT NULL, MaxLength(50) | 데이터 출처 (GoogleBooks, TMDb 등) |
| CreatedAt | DateTime | NOT NULL, DEFAULT CURRENT_TIMESTAMP | 생성 시각 (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT CURRENT_TIMESTAMP | 최종 수정 시각 (UTC) |

**인덱스**:
- `idx_barcode`: Barcode (UNIQUE) - 바코드 조회 최적화
- `idx_media_type`: MediaType - 타입별 필터링

**C# 구현**:
```csharp
public abstract class MediaItem
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum MediaType
{
    Book = 1,
    Movie = 2,
    MusicAlbum = 3
}
```

---

### 2. Book (도서)

**용도**: 도서 특화 정보 (ISBN, 저자, 출판사 등)

**필드**:

| 필드명 | 타입 | 제약 조건 | 설명 |
|--------|------|----------|------|
| MediaItemId | Guid | PK, FK(MediaItem.Id) | 기본 키 (MediaItem 참조) |
| Isbn13 | string | NOT NULL, MaxLength(13) | ISBN-13 정규화 형식 |
| Authors | string[] | NOT NULL | 저자 목록 (PostgreSQL text[]) |
| Publisher | string? | NULL, MaxLength(255) | 출판사 |
| PublishDate | DateOnly? | NULL | 출판일 |
| PageCount | int? | NULL, > 0 | 페이지 수 |
| Genre | string? | NULL, MaxLength(100) | 장르 (소설, 비소설, 과학 등) |

**검증 규칙**:
- `Isbn13`: 13자리 숫자, 978 또는 979로 시작
- `Authors`: 최소 1명 이상
- `PageCount`: 양수

**C# 구현**:
```csharp
public class Book : MediaItem
{
    public string Isbn13 { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = new();
    public string? Publisher { get; set; }
    public DateOnly? PublishDate { get; set; }
    public int? PageCount { get; set; }
    public string? Genre { get; set; }
    
    public Book()
    {
        MediaType = MediaType.Book;
    }
}
```

**EF Core Configuration**:
```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Isbn13)
            .IsRequired()
            .HasMaxLength(13);
        
        builder.Property(b => b.Authors)
            .HasColumnType("text[]")  // PostgreSQL array
            .IsRequired();
        
        builder.Property(b => b.Publisher)
            .HasMaxLength(255);
        
        builder.Property(b => b.Genre)
            .HasMaxLength(100);
    }
}
```

---

### 3. Movie (영화 - Blu-ray/DVD)

**용도**: 영화/TV 프로그램 특화 정보

**필드**:

| 필드명 | 타입 | 제약 조건 | 설명 |
|--------|------|----------|------|
| MediaItemId | Guid | PK, FK(MediaItem.Id) | 기본 키 |
| Director | string? | NULL, MaxLength(255) | 감독 |
| Cast | string[] | NULL | 출연진 목록 (PostgreSQL text[]) |
| RuntimeMinutes | int? | NULL, > 0 | 상영 시간 (분) |
| ReleaseDate | DateOnly? | NULL | 개봉일 |
| Rating | string? | NULL, MaxLength(10) | 등급 (G, PG, PG-13, R 등) |
| Genre | string? | NULL, MaxLength(100) | 장르 (액션, 드라마, 코미디 등) |

**검증 규칙**:
- `RuntimeMinutes`: 양수, 일반적으로 30~300분
- `Rating`: 표준 등급 값 (G, PG, PG-13, R, NC-17)

**C# 구현**:
```csharp
public class Movie : MediaItem
{
    public string? Director { get; set; }
    public List<string> Cast { get; set; } = new();
    public int? RuntimeMinutes { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Rating { get; set; }
    public string? Genre { get; set; }
    
    public Movie()
    {
        MediaType = MediaType.Movie;
    }
}
```

**EF Core Configuration**:
```csharp
public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("movies");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Director)
            .HasMaxLength(255);
        
        builder.Property(m => m.Cast)
            .HasColumnType("text[]");
        
        builder.Property(m => m.Rating)
            .HasMaxLength(10);
        
        builder.Property(m => m.Genre)
            .HasMaxLength(100);
    }
}
```

---

### 4. MusicAlbum (음악 앨범)

**용도**: 음악 앨범 특화 정보

**필드**:

| 필드명 | 타입 | 제약 조건 | 설명 |
|--------|------|----------|------|
| MediaItemId | Guid | PK, FK(MediaItem.Id) | 기본 키 |
| Artist | string | NOT NULL, MaxLength(255) | 아티스트/밴드 |
| Tracks | Track[] | NULL | 트랙 목록 (PostgreSQL JSONB) |
| ReleaseDate | DateOnly? | NULL | 발매일 |
| Label | string? | NULL, MaxLength(255) | 레이블/레코드사 |
| Genre | string? | NULL, MaxLength(100) | 장르 (Rock, Pop, Jazz 등) |

**Tracks 구조** (JSONB):
```json
[
  {
    "trackNumber": 1,
    "title": "Song Title",
    "durationSeconds": 180
  },
  {
    "trackNumber": 2,
    "title": "Another Song",
    "durationSeconds": 240
  }
]
```

**검증 규칙**:
- `Artist`: 필수
- `Tracks`: TrackNumber는 1부터 시작하는 순차 번호
- `DurationSeconds`: 양수

**C# 구현**:
```csharp
public class MusicAlbum : MediaItem
{
    public string Artist { get; set; } = string.Empty;
    public List<Track> Tracks { get; set; } = new();
    public DateOnly? ReleaseDate { get; set; }
    public string? Label { get; set; }
    public string? Genre { get; set; }
    
    public MusicAlbum()
    {
        MediaType = MediaType.MusicAlbum;
    }
}

public class Track
{
    public int TrackNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
}
```

**EF Core Configuration**:
```csharp
public class MusicAlbumConfiguration : IEntityTypeConfiguration<MusicAlbum>
{
    public void Configure(EntityTypeBuilder<MusicAlbum> builder)
    {
        builder.ToTable("music_albums");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Artist)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(m => m.Tracks)
            .HasColumnType("jsonb")  // PostgreSQL JSONB
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<Track>>(v, (JsonSerializerOptions)null!)!
            );
        
        builder.Property(m => m.Label)
            .HasMaxLength(255);
        
        builder.Property(m => m.Genre)
            .HasMaxLength(100);
    }
}
```

---

## 데이터베이스 마이그레이션

### 초기 마이그레이션 생성

```bash
cd src/CollectionServer.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../CollectionServer.Api
dotnet ef database update --startup-project ../CollectionServer.Api
```

### 생성될 SQL (PostgreSQL)

```sql
-- MediaItems 테이블 (기본 엔티티)
CREATE TABLE media_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    barcode VARCHAR(13) NOT NULL UNIQUE,
    media_type INT NOT NULL,
    title VARCHAR(500) NOT NULL,
    description VARCHAR(5000),
    image_url VARCHAR(2000),
    source VARCHAR(50) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_barcode ON media_items (barcode);
CREATE INDEX idx_media_type ON media_items (media_type);

-- Books 테이블 (TPT)
CREATE TABLE books (
    id UUID PRIMARY KEY,
    isbn13 VARCHAR(13) NOT NULL,
    authors TEXT[] NOT NULL,
    publisher VARCHAR(255),
    publish_date DATE,
    page_count INT CHECK (page_count > 0),
    genre VARCHAR(100),
    FOREIGN KEY (id) REFERENCES media_items(id) ON DELETE CASCADE
);

-- Movies 테이블 (TPT)
CREATE TABLE movies (
    id UUID PRIMARY KEY,
    director VARCHAR(255),
    cast TEXT[],
    runtime_minutes INT CHECK (runtime_minutes > 0),
    release_date DATE,
    rating VARCHAR(10),
    genre VARCHAR(100),
    FOREIGN KEY (id) REFERENCES media_items(id) ON DELETE CASCADE
);

-- MusicAlbums 테이블 (TPT)
CREATE TABLE music_albums (
    id UUID PRIMARY KEY,
    artist VARCHAR(255) NOT NULL,
    tracks JSONB,
    release_date DATE,
    label VARCHAR(255),
    genre VARCHAR(100),
    FOREIGN KEY (id) REFERENCES media_items(id) ON DELETE CASCADE
);

-- UpdatedAt 자동 업데이트 트리거
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_media_items_updated_at
    BEFORE UPDATE ON media_items
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

---

## 리포지토리 패턴

### IMediaRepository 인터페이스

```csharp
public interface IMediaRepository
{
    Task<MediaItem?> GetByBarcodeAsync(string barcode);
    Task<MediaItem> AddAsync(MediaItem item);
    Task<MediaItem> UpdateAsync(MediaItem item);
    Task<bool> ExistsAsync(string barcode);
    Task<IEnumerable<MediaItem>> GetRecentAsync(int count = 10);
}
```

### MediaRepository 구현

```csharp
public class MediaRepository : IMediaRepository
{
    private readonly ApplicationDbContext _context;
    
    public MediaRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<MediaItem?> GetByBarcodeAsync(string barcode)
    {
        return await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Barcode == barcode);
    }
    
    public async Task<MediaItem> AddAsync(MediaItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        
        _context.MediaItems.Add(item);
        await _context.SaveChangesAsync();
        
        return item;
    }
    
    public async Task<MediaItem> UpdateAsync(MediaItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        
        _context.MediaItems.Update(item);
        await _context.SaveChangesAsync();
        
        return item;
    }
    
    public async Task<bool> ExistsAsync(string barcode)
    {
        return await _context.MediaItems
            .AnyAsync(m => m.Barcode == barcode);
    }
    
    public async Task<IEnumerable<MediaItem>> GetRecentAsync(int count = 10)
    {
        return await _context.MediaItems
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
```

---

## 상태 전환

### 데이터 생명주기

```text
[외부 API에서 새 데이터]
           ↓
    [검증 및 정규화]
           ↓
    [데이터베이스 저장]
           ↓
    [CreatedAt 타임스탬프]
           ↓
    [이후 요청 시 DB에서 조회]
           ↓
    [UpdatedAt 업데이트 (데이터 갱신 시)]
```

**불변 필드**: `Id`, `Barcode`, `CreatedAt`  
**가변 필드**: `Title`, `Description`, `ImageUrl`, `UpdatedAt`, 기타 메타데이터

---

## 성능 고려사항

### 인덱싱 전략

1. **Barcode (UNIQUE, BTREE)**: 
   - 가장 빈번한 조회 필드
   - 고유 제약 조건으로 중복 방지

2. **MediaType (BTREE)**:
   - 타입별 통계 쿼리 최적화
   - `SELECT COUNT(*) FROM media_items WHERE media_type = 1`

3. **CreatedAt (BTREE)**:
   - 최근 추가된 미디어 조회
   - 시계열 분석

### 쿼리 최적화

**1. Compiled Queries (EF Core 10.0)**:
```csharp
private static readonly Func<ApplicationDbContext, string, Task<MediaItem?>> GetByBarcodeCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext context, string barcode) =>
        context.MediaItems.FirstOrDefault(m => m.Barcode == barcode));

public async Task<MediaItem?> GetByBarcodeAsync(string barcode)
{
    return await GetByBarcodeCompiled(_context, barcode);
}
```

**2. AsNoTracking (읽기 전용)**:
```csharp
public async Task<MediaItem?> GetByBarcodeReadOnlyAsync(string barcode)
{
    return await _context.MediaItems
        .AsNoTracking()  // 변경 추적 비활성화
        .FirstOrDefaultAsync(m => m.Barcode == barcode);
}
```

**3. Select Projection (필요한 필드만)**:
```csharp
public async Task<MediaSummary> GetSummaryAsync(string barcode)
{
    return await _context.MediaItems
        .Where(m => m.Barcode == barcode)
        .Select(m => new MediaSummary
        {
            Title = m.Title,
            ImageUrl = m.ImageUrl
        })
        .FirstOrDefaultAsync();
}
```

---

## 데이터 검증

### 바코드 검증

```csharp
public class BarcodeValidator
{
    public static bool ValidateIsbn13(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsDigit))
            return false;
        
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = isbn[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        
        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (isbn[12] - '0');
    }
    
    public static bool ValidateUpc(string upc)
    {
        if (upc.Length != 12 || !upc.All(char.IsDigit))
            return false;
        
        int sum = 0;
        for (int i = 0; i < 11; i++)
        {
            int digit = upc[i] - '0';
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }
        
        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (upc[11] - '0');
    }
}
```

---

## 테스트 데이터

### 시드 데이터 (개발 환경)

```csharp
public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.MediaItems.Any()) return;
        
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Barcode = "9780134685991",
            Title = "Effective Java",
            Description = "The definitive guide to Java programming",
            Authors = new List<string> { "Joshua Bloch" },
            Publisher = "Addison-Wesley",
            PublishDate = new DateOnly(2018, 1, 6),
            PageCount = 416,
            Source = "GoogleBooks"
        };
        
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Barcode = "883929641680",
            Title = "Inception",
            Description = "A thief who steals corporate secrets through dream-sharing technology",
            Director = "Christopher Nolan",
            Cast = new List<string> { "Leonardo DiCaprio", "Ellen Page", "Tom Hardy" },
            RuntimeMinutes = 148,
            ReleaseDate = new DateOnly(2010, 7, 16),
            Rating = "PG-13",
            Source = "TMDb"
        };
        
        context.MediaItems.AddRange(book, movie);
        context.SaveChanges();
    }
}
```

---

## 요약

### 핵심 설계 결정

1. **TPT (Table Per Type) 전략**: 각 미디어 타입별 전용 테이블
2. **PostgreSQL 고급 기능**: 배열(text[]), JSONB
3. **Database-First 최적화**: Barcode 인덱스, Compiled Queries
4. **Clean Separation**: Repository 패턴으로 데이터 접근 추상화
5. **타입 안전성**: C# 13 강타입 + EF Core Configuration

### 다음 단계

- **contracts/**: OpenAPI 스키마 생성
- **quickstart.md**: 개발 환경 설정 및 마이그레이션 가이드
