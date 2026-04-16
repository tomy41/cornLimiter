namespace CornLimiter.Application.Exceptions;

public class RateLimitException :Exception
{
    public const string DefaultMessage = "Limit per time to buy was exceeded. Please try again later.";
    public RateLimitException() : base(DefaultMessage)  { }
}
