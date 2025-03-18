using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace ScienceBirdTweaks
{
    public class LobbyCompatibility
    {
        public static void RegisterCompatibility()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value)
            {
                PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, System.Version.Parse(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.ClientOnly, VersionStrictness.None);
            }
            else
            {
                PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, System.Version.Parse(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.Everyone, VersionStrictness.None);
            }
        }
    }
}
