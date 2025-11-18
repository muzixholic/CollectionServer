using CollectionServer.Core.Entities;
using CollectionServer.Core.Exceptions;
using CollectionServer.Core.Interfaces;

namespace CollectionServer.Core.Services;

/// <summary>
/// 미디어 서비스 구현
/// Database-First 아키텍처: 데이터베이스 우선 조회 후 외부 API 폴백
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaRepository _repository;
    private readonly BarcodeValidator _validator;

    public MediaService(IMediaRepository repository, BarcodeValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    /// <summary>
    /// 바코드로 미디어 항목 조회
    /// Database-First: 먼저 데이터베이스 조회, 없으면 외부 API 호출 (Phase 4에서 구현)
    /// </summary>
    public async Task<MediaItem> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // 1. 바코드 검증
        _validator.Validate(barcode);

        // 2. 데이터베이스 조회
        var mediaItem = await _repository.GetByBarcodeAsync(barcode, cancellationToken);
        
        if (mediaItem != null)
        {
            return mediaItem;
        }

        // 3. 외부 API 조회 (Phase 4에서 구현 예정)
        // TODO: Phase 4에서 외부 API Provider 통합
        
        // 4. 모든 소스에서 찾지 못함
        throw new NotFoundException("미디어", barcode);
    }

    /// <summary>
    /// ID로 미디어 항목 조회
    /// </summary>
    public async Task<MediaItem?> GetMediaByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // TODO: Repository에 GetByIdAsync 메서드 추가 필요
        throw new NotImplementedException("Phase 4에서 구현 예정");
    }
}
