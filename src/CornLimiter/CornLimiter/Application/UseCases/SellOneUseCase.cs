using AutoMapper;
using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Application.Service;
using CornLimiter.Configuration;
using CornLimiter.Domain;
using CornLimiter.Domain.Models;
using CornLimiter.Domain.ValueObjects;
using CornLimiter.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CornLimiter.Application.UseCases;

public class SellOneUseCase
{

    private bool _cacheBoostEnabled = true;
    private readonly IMemoryCache _memoryCache;
    private readonly ISaleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOptions<CornLimiterOptions> _options;
    public SellOneUseCase(IMemoryCache memoryCache,
    ISaleRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOptions<CornLimiterOptions> options,
    IFeatureFlagsService featureFlagsService)
    {
        _memoryCache = memoryCache;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _options = options;
        _cacheBoostEnabled = featureFlagsService.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag");
    }

    public async Task<SaleDto> ExecuteAsync(SellOneCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await ValidateRateLimitAsync(command, cancellationToken);

        if (_cacheBoostEnabled)
        {
            var sale = await _memoryCache.GetOrCreateAsync(command.FarmerCode, async entry =>
            {
                var cacheMinutes = Math.Max(1, _options.Value.MinutesLimitToAllowNewSale);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes);
                return await AddSale(command, cancellationToken);
            });
            return _mapper.Map<SaleDto>(sale);
        }
        else
        {
            var sale = await AddSale(command, cancellationToken);
            return _mapper.Map<SaleDto>(sale);
        }

    }

    private async Task<Sale> AddSale(SellOneCommand command, CancellationToken cancellationToken)
    {
        var sale = Sale.Create(command.FarmerCode);
        await _repository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return sale;
    }

    private async Task ValidateRateLimitAsync(SellOneCommand command, CancellationToken cancellationToken)
    {
        if (_cacheBoostEnabled && _memoryCache.TryGetValue(command.FarmerCode, out var _))
        {
            throw new RateLimitException();
        }

        var lastSale = await _repository.GetLastAsync(command.FarmerCode, cancellationToken);
        if (lastSale is not null && lastSale.SoldOnUtc.AddMinutes(_options.Value.MinutesLimitToAllowNewSale) >= DateTime.UtcNow)
        {
            throw new RateLimitException();
        }
    }
}
