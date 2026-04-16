using CornLimiter.Domain.Models;

namespace CornLimiter.Domain;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetLastAsync(Guid farmerCode, CancellationToken cancellationToken = default);
    Task<List<Sale>> ListByFarmerCodeAsync(Guid farmerCode, CancellationToken cancellationToken = default);
}