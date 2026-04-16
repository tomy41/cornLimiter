namespace CornLimiter.Domain.Models;

public class Sale
{
    public int Id { get; set; }

    public Guid FarmerCode { get; set; }

    public DateTime SoldOnUtc { get; set; }

    public static Sale Create(Guid farmerCode)
    {
        return new Sale
        {
            FarmerCode = farmerCode,
            SoldOnUtc = DateTime.UtcNow
        };
    }
}