using CornLimiter.Application.Queries;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases
{
    public interface IListSellingsByFarmerUseCase
    {
        Task<IEnumerable<SaleDto>> ExecuteAsync(SalesByFarmerQuery query, CancellationToken cancellationToken = default);
    }
}