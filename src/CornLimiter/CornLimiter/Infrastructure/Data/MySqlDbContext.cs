using CornLimiter.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CornLimiter.Infrastructure.Data;

public class MySqlDbContext(DbContextOptions<MySqlDbContext> options) : DbContext(options), IUnitOfWork
{
    
    public DbSet<Sale> Sales { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable(nameof(Sale));

            // Id será la PK autogenerada por la base de datos (auto-increment)
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();

            // FarmerCode sigue existiendo como GUID generado por la BD por defecto
            entity.Property(e => e.FarmerCode)
                  .HasColumnType("char(36)")
                  .IsRequired();

            entity.Property(e => e.SoldOnUtc)
                  .IsRequired();
        });
    }
}