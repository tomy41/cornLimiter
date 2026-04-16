namespace CornLimiter.Domain
{
    public interface IFeatureFlagsService
    {
        bool IsFeatureFlagEnabled(string flagKey);
    }
}
