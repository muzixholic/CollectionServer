namespace CollectionServer.Core.Exceptions;

/// <summary>
/// 리소스를 찾을 수 없을 때 발생하는 예외
/// </summary>
public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId, string message) 
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string resourceType, string resourceId) 
        : base($"{resourceType}을(를) 찾을 수 없습니다: {resourceId}")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
