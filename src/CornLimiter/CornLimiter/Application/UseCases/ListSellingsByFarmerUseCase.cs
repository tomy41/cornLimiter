using AutoMapper;
using CornLimiter.Application.Queries;
using CornLimiter.Domain;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases;

public class ListSellingsByFarmerUseCase(ISaleRepository salesRepository, IMapper mapper) : IListSellingsByFarmerUseCase
{
    private readonly ISaleRepository _saleRepository = salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<IEnumerable<SaleDto>> ExecuteAsync(SalesByFarmerQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sellings = await _saleRepository.ListByFarmerCodeAsync(query.FarmerCode, cancellationToken);
        return [.. sellings.Select(_mapper.Map<SaleDto>)];
    }

}
