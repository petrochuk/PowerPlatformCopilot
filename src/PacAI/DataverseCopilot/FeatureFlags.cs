using bolt.system;

namespace DataverseCopilot;

internal class FeatureFlags : IFeatureFlags
{
    public bool IsFeatureEnabled(FeatureName featureName)
    {
        return true;
    }
}
