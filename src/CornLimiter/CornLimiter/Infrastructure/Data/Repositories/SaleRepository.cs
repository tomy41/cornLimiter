using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CornLimiter.Domain;
using CornLimiter.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CornLimiter.Infrastructure.Data.Repositories;

public class SaleRepository(MySqlDbContext db) : ISaleRepository
{
    private readonly MySqlDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        await _db.Sales.AddAsync(sale, cancellationToken);        
    }

    public async Task<List<Sale>> ListByFarmerCodeAsync(Guid farmerCode, CancellationToken cancellationToken = default)
    {
        return await _db.Sales
                    .Where(p => p.FarmerCode == farmerCode)
                    .OrderByDescending(p => p.SoldOnUtc)
                    .ToListAsync(cancellationToken);
    }

    public async Task<Sale?> GetLastAsync(Guid farmerCode, CancellationToken cancellationToken = default)
    {
        return await _db.Sales
                        .Where(p => p.FarmerCode == farmerCode)
                        .OrderByDescending(p => p.SoldOnUtc)
                        .FirstOrDefaultAsync(cancellationToken);
    }
}
