using LaunchDarkly.Sdk.Server;

namespace CornLimiter.Application.Service
{
    public interface IFeatureFlagsService
    {
        bool IsFeatureFlagEnabled(string flagKey);
    }

    public class FeatureFlagService(LdClient client) : IFeatureFlagsService
    {

        public bool IsFeatureFlagEnabled(string flagKey)
        {
            var context = LaunchDarkly.Sdk.Context.Builder("anonymous")
                .Name("anonymous")
                .Build();
            bool isFeatureEnabled = client.BoolVariation(flagKey, context, false);

            return isFeatureEnabled;
        }
    }
}
