namespace CornLimiter.Configuration;

public class CornLimiterOptions
{
    /// <summary>
    /// Duration in minutes for a farmer to be allowed to buy a unit.
    /// Default value is 1.
    /// </summary>
    public int MinutesLimitToAllowNewSale { get; set; } = 1;
}