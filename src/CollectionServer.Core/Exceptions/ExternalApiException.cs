namespace CollectionServer.Core.Exceptions;

/// <summary>
/// 외부 API 호출 중 발생하는 예외
/// </summary>
public class ExternalApiException : Exception
{
    public string ProviderName { get; }
    public int? StatusCode { get; }

    public ExternalApiException(string providerName, string message) 
        : base(message)
    {
        ProviderName = providerName;
    }

    public ExternalApiException(string providerName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
    
    public ExternalApiException(string providerName, int statusCode, string message) 
        : base(message)
    {
        ProviderName = providerName;
        StatusCode = statusCode;
    }
}
