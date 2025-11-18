namespace CollectionServer.Core.Enums;

/// <summary>
/// 바코드 유형을 나타내는 열거형
/// </summary>
public enum BarcodeType
{
    /// <summary>
    /// ISBN-10 (10자리 국제 표준 도서 번호)
    /// </summary>
    ISBN10 = 1,

    /// <summary>
    /// ISBN-13 (13자리 국제 표준 도서 번호)
    /// </summary>
    ISBN13 = 2,

    /// <summary>
    /// UPC (12자리 범용 제품 코드)
    /// </summary>
    UPC = 3,

    /// <summary>
    /// EAN-13 (13자리 유럽 상품 번호)
    /// </summary>
    EAN13 = 4
}
