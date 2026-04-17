using CornLimiter.Application.Queries;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases;

public interface IListSalesByFarmerUseCase
{
    Task<IEnumerable<SaleDto>> ExecuteAsync(ListSalesByFarmerQuery query, CancellationToken cancellationToken = default);
}