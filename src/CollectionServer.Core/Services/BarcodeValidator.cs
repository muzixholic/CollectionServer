using CollectionServer.Core.Enums;
using CollectionServer.Core.Exceptions;

namespace CollectionServer.Core.Services;

/// <summary>
/// 바코드 검증 서비스
/// ISBN-10/13, UPC, EAN-13 체크섬 검증 포함
/// </summary>
public class BarcodeValidator
{
    /// <summary>
    /// 바코드 검증 및 유형 반환
    /// </summary>
    public BarcodeType Validate(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new InvalidBarcodeException(barcode, "바코드가 비어 있습니다.");
        }

        // 숫자와 X만 추출 (공백, 하이픈 제거)
        var cleaned = new string(barcode.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
        
        // 소문자 x를 대문자 X로 변환
        cleaned = cleaned.Replace('x', 'X');

        return cleaned.Length switch
        {
            10 => ValidateIsbn10(cleaned),
            12 => ValidateUpc(cleaned),
            13 => ValidateIsbn13OrEan13(cleaned),
            _ => throw new InvalidBarcodeException(barcode, $"지원하지 않는 바코드 길이입니다: {cleaned.Length}자리. ISBN-10(10), UPC(12), ISBN-13/EAN-13(13)만 지원합니다.")
        };
    }

    private static BarcodeType ValidateIsbn10(string isbn10)
    {
        // ISBN-10 체크섬 검증 (Modulus 11)
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (isbn10[i] - '0') * (10 - i);
        }

        char checkChar = isbn10[9];
        int checkDigit = checkChar == 'X' ? 10 : (checkChar - '0');

        sum += checkDigit;

        if (sum % 11 != 0)
        {
            throw new InvalidBarcodeException(isbn10, "ISBN-10 체크섬이 유효하지 않습니다.");
        }

        return BarcodeType.ISBN10;
    }

    private static BarcodeType ValidateUpc(string upc)
    {
        // UPC 체크섬 검증 (Modulus 10)
        int oddSum = 0;
        int evenSum = 0;

        for (int i = 0; i < 11; i++)
        {
            int digit = upc[i] - '0';
            if (i % 2 == 0)
                oddSum += digit;
            else
                evenSum += digit;
        }

        int checksum = (10 - ((oddSum * 3 + evenSum) % 10)) % 10;
        int actualCheck = upc[11] - '0';

        if (checksum != actualCheck)
        {
            throw new InvalidBarcodeException(upc, "UPC 체크섬이 유효하지 않습니다.");
        }

        return BarcodeType.UPC;
    }

    private static BarcodeType ValidateIsbn13OrEan13(string code)
    {
        // ISBN-13/EAN-13 체크섬 검증 (Modulus 10)
        int oddSum = 0;
        int evenSum = 0;

        for (int i = 0; i < 12; i++)
        {
            int digit = code[i] - '0';
            if (i % 2 == 0)
                oddSum += digit;
            else
                evenSum += digit;
        }

        int checksum = (10 - ((oddSum * 1 + evenSum * 3) % 10)) % 10;
        int actualCheck = code[12] - '0';

        if (checksum != actualCheck)
        {
            throw new InvalidBarcodeException(code, "ISBN-13/EAN-13 체크섬이 유효하지 않습니다.");
        }

        // ISBN-13은 978 또는 979로 시작
        if (code.StartsWith("978") || code.StartsWith("979"))
        {
            return BarcodeType.ISBN13;
        }

        return BarcodeType.EAN13;
    }

    /// <summary>
    /// 바코드 유형으로 미디어 타입 추정
    /// </summary>
    public MediaType? InferMediaType(BarcodeType barcodeType)
    {
        return barcodeType switch
        {
            BarcodeType.ISBN10 => MediaType.Book,
            BarcodeType.ISBN13 => MediaType.Book,
            BarcodeType.UPC => null, // UPC는 여러 미디어 타입 가능
            BarcodeType.EAN13 => null, // EAN-13도 여러 미디어 타입 가능
            _ => null
        };
    }
}
