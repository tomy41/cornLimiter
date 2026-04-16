using CornLimiter.Application.Commands;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Application.UseCases
{
    public interface ISellOneUseCase
    {
        Task<SaleDto> ExecuteAsync(SellOneCommand command, CancellationToken cancellationToken = default);
    }
}