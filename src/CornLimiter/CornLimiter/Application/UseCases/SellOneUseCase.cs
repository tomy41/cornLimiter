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

public class SellOneUseCase(IMemoryCache memoryCache, ISaleRepository repository, IUnitOfWork unitOfWork, IMapper mapper, IOptions<CornLimiterOptions> options)
{
    public async Task<SaleDto> ExecuteAsync(SellOneCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        await ValidateRateLimitAsync(command, cancellationToken);

        var sale = await AddSale(command, cancellationToken);

        var cacheMinutes = Math.Max(1, options.Value.MinutesLimitToAllowNewSale);
        memoryCache.Set(command.FarmerCode, command.FarmerCode, TimeSpan.FromMinutes(cacheMinutes));

        return mapper.Map<SaleDto>(sale);
    }

    private async Task<Sale> AddSale(SellOneCommand command, CancellationToken cancellationToken)
    {
        var sale = Sale.Create(command.FarmerCode);
        await repository.AddAsync(sale, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return sale;
    }

    private async Task ValidateRateLimitAsync(SellOneCommand command, CancellationToken cancellationToken)
    {
        if (memoryCache.TryGetValue(command.FarmerCode, out var _))
        {
            throw new RateLimitException();
        }

        var lastSale = await repository.GetLastAsync(command.FarmerCode, cancellationToken);
        if (lastSale is not null && lastSale.SoldOnUtc > DateTime.UtcNow.AddMinutes(options.Value.MinutesLimitToAllowNewSale))
        {
            throw new RateLimitException();
        }
    }
}
