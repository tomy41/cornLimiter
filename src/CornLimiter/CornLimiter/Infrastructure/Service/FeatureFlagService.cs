using CornLimiter.Domain;
using LaunchDarkly.Sdk.Server;

namespace CornLimiter.Infrastructure.Service;

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
