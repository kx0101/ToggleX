using ToggleX.Models;

namespace ToggleX.Providers
{
    public interface IFeatureProvider

    {
        List<FeatureFlag> GetFeatureFlags();
    }
}
