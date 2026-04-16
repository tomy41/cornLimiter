using AutoMapper;
using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Configuration;
using CornLimiter.Domain;
using CornLimiter.Domain.Models;
using CornLimiter.Domain.ValueObjects;
using CornLimiter.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CornLimiter.Application.UseCases;

public class SellOneUseCase(IMemoryCache memoryCache,
    ISaleRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOptions<CornLimiterOptions> options,
    IFeatureFlagsService featureFlagsService) : ISellOneUseCase
{
    private readonly bool _cacheBoostEnabled = featureFlagsService?.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag") ?? throw new ArgumentNullException(nameof(featureFlagsService));
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly ISaleRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IOptions<CornLimiterOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

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
