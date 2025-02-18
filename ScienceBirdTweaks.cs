using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ScienceBirdTweaks : BaseUnityPlugin
    {
        public static ScienceBirdTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle TweaksAssets;
        public static ConfigEntry<bool> ConsistentRailingCollision;
        public static ConfigEntry<bool> BigScrew;
        public static ConfigEntry<bool> DustSpaceClouds;
        public static ConfigEntry<bool> DiversityComputerBegone;
        public static ConfigEntry<bool> OldHalloweenElevatorMusic;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ConsistentRailingCollision = base.Config.Bind("Tweaks", "Consistent Railing Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk.");
            BigScrew = base.Config.Bind("Tweaks", "Big Screw", true, "'Big bolt' is accurately renamed to 'Big screw'.");
            DustSpaceClouds = base.Config.Bind("Tweaks", "Dust (Space) Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (only affects modded content which uses it, since it's never explicitly displayed in vanilla).");
            DiversityComputerBegone = base.Config.Bind("Tweaks", "Diversity Computer Begone", true, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");
            OldHalloweenElevatorMusic = base.Config.Bind("Tweaks", "Old Halloween Elevator Music", true, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            TweaksAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "tweaksassets"));

            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
