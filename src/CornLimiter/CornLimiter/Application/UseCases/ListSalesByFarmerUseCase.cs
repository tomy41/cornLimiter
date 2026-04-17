using AutoMapper;
using CornLimiter.Application.Queries;
using CornLimiter.Domain;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases;

public class ListSalesByFarmerUseCase(ISaleRepository salesRepository, IMapper mapper) : IListSalesByFarmerUseCase
{
    private readonly ISaleRepository _saleRepository = salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<IEnumerable<SaleDto>> ExecuteAsync(ListSalesByFarmerQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sales = await _saleRepository.ListByFarmerCodeAsync(query.FarmerCode, cancellationToken);
        return sales.Select(s => _mapper.Map<SaleDto>(s));
    }

}
