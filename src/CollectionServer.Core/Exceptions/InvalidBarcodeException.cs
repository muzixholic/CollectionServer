namespace CollectionServer.Core.Exceptions;

/// <summary>
/// 잘못된 바코드 형식 또는 체크섬 오류 예외
/// </summary>
public class InvalidBarcodeException : Exception
{
    public string Barcode { get; }

    public InvalidBarcodeException(string message) 
        : base(message)
    {
        Barcode = string.Empty;
    }

    public InvalidBarcodeException(string barcode, string message) 
        : base(message)
    {
        Barcode = barcode;
    }

    public InvalidBarcodeException(string barcode, string message, Exception innerException) 
        : base(message, innerException)
    {
        Barcode = barcode;
    }
}
