namespace CollectionServer.Infrastructure.Options;

/// <summary>
/// 외부 API 설정 (Options 패턴)
/// </summary>
public class ExternalApiSettings
{
    public GoogleBooksSettings GoogleBooks { get; set; } = new();
    public KakaoBookSettings KakaoBook { get; set; } = new();
    public AladinApiSettings AladinApi { get; set; } = new();
    public TMDbSettings TMDb { get; set; } = new();
    public OMDbSettings OMDb { get; set; } = new();
    public MusicBrainzSettings MusicBrainz { get; set; } = new();
    public DiscogsSettings Discogs { get; set; } = new();
    public UpcItemDbSettings UpcItemDb { get; set; } = new();
}

public class GoogleBooksSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.googleapis.com/books/v1";
    public int Priority { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 10;
}

public class KakaoBookSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://dapi.kakao.com";
    public int Priority { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 10;
}

public class AladinApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://www.aladin.co.kr/ttb/api";
    public int Priority { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 10;
}

public class TMDbSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";
    public int Priority { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 10;
}

public class OMDbSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://www.omdbapi.com";
    public int Priority { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 10;
}

public class MusicBrainzSettings
{
    public string BaseUrl { get; set; } = "https://musicbrainz.org/ws/2";
    public string UserAgent { get; set; } = "CollectionServer/1.0";
    public int Priority { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 10;
}

public class DiscogsSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.discogs.com";
    public int Priority { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 10;
}

public class UpcItemDbSettings
{
    public string BaseUrl { get; set; } = "https://api.upcitemdb.com/prod/trial";
    public int Priority { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 10;
}
