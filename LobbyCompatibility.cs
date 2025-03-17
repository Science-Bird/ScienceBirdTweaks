using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace ScienceBirdTweaks
{
    public class LobbyCompatibility
    {
        public static void RegisterCompatibility()
        {
            PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, System.Version.Parse(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.Everyone, VersionStrictness.None);
        }
    }
}
