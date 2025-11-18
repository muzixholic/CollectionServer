namespace CollectionServer.Core.Exceptions;

/// <summary>
/// Rate Limit 초과 시 발생하는 예외
/// </summary>
public class RateLimitExceededException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds, string message) 
        : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public RateLimitExceededException(int retryAfterSeconds) 
        : base($"요청 제한을 초과했습니다. {retryAfterSeconds}초 후에 다시 시도하세요.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
