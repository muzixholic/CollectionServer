using CollectionServer.Core.Entities;
using CollectionServer.Core.Exceptions;
using CollectionServer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CollectionServer.Core.Services;

/// <summary>
/// 미디어 서비스 구현
/// Database-First 아키텍처: 데이터베이스 우선 조회 후 외부 API 폴백
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaRepository _repository;
    private readonly BarcodeValidator _validator;
    private readonly IEnumerable<IMediaProvider> _providers;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository repository, 
        BarcodeValidator validator,
        IEnumerable<IMediaProvider> providers,
        ILogger<MediaService> logger)
    {
        _repository = repository;
        _validator = validator;
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// 바코드로 미디어 항목 조회
    /// Database-First: 먼저 데이터베이스 조회, 없으면 외부 API 호출
    /// </summary>
    public async Task<MediaItem> GetMediaByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // 1. 바코드 검증
        _validator.Validate(barcode);

        // 2. 데이터베이스 조회 (Database-First)
        var mediaItem = await _repository.GetByBarcodeAsync(barcode, cancellationToken);
        
        if (mediaItem != null)
        {
            _logger.LogInformation("Found media item in database for barcode: {Barcode}", barcode);
            return mediaItem;
        }

        // 3. 외부 API 우선순위 기반 폴백
        _logger.LogInformation("Media item not found in database, trying external providers for barcode: {Barcode}", barcode);
        
        _logger.LogInformation("Total providers registered: {Count}", _providers.Count());

        var supportedProviders = _providers
            .Where(p => {
                var supports = p.SupportsBarcode(barcode);
                _logger.LogInformation("Provider {Name} supports barcode {Barcode}: {Supports}", 
                    p.ProviderName, barcode, supports);
                return supports;
            })
            .OrderBy(p => p.Priority)
            .ToList();

        _logger.LogInformation("Found {Count} supported providers for barcode: {Barcode}", 
            supportedProviders.Count, barcode);

        if (supportedProviders.Count == 0)
        {
            _logger.LogWarning("No providers support barcode format: {Barcode}", barcode);
            throw new NotFoundException("미디어", barcode);
        }

        foreach (var provider in supportedProviders)
        {
            try
            {
                _logger.LogInformation("Trying provider {Provider} (Priority: {Priority}) for barcode: {Barcode}", 
                    provider.ProviderName, provider.Priority, barcode);

                var result = await provider.GetMediaByBarcodeAsync(barcode, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("Successfully retrieved media from provider {Provider}: {Title}", 
                        provider.ProviderName, result.Title);

                    // 4. 데이터베이스에 저장 (캐싱)
                    await _repository.AddAsync(result, cancellationToken);
                    _logger.LogInformation("Saved media item to database for future queries: {Barcode}", barcode);
                    
                    return result;
                }
                else
                {
                    _logger.LogInformation("Provider {Provider} returned no results for barcode: {Barcode}", 
                        provider.ProviderName, barcode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for barcode: {Barcode}", 
                    provider.ProviderName, barcode);
                // Continue to next provider
            }
        }
        
        // 5. 모든 소스에서 찾지 못함
        _logger.LogWarning("All providers failed to find media for barcode: {Barcode}", barcode);
        throw new NotFoundException("미디어", barcode);
    }

    /// <summary>
    /// ID로 미디어 항목 조회
    /// </summary>
    public async Task<MediaItem?> GetMediaByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }
}
