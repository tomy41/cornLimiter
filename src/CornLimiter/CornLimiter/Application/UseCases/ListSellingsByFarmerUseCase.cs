using AutoMapper;
using CornLimiter.Application.Queries;
using CornLimiter.Domain;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases;

public class ListSellingsByFarmerUseCase(ISaleRepository repository, IMapper mapper)
{
    public async Task<IEnumerable<SaleDto>> ExecuteAsync(SalesByFarmerQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sellings = await repository.ListByFarmerCodeAsync(query.FarmerCode, cancellationToken);
        return [.. sellings.Select(mapper.Map<SaleDto>)];
    }

}
