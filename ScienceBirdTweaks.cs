using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ScienceBirdTweaks.Patches;
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
        public static ConfigEntry<bool> FixedTeleporterButton;
        public static ConfigEntry<bool> BigScrew;
        public static ConfigEntry<bool> DustSpaceClouds;
        public static ConfigEntry<bool> FallingRotationFix;
        public static ConfigEntry<bool> TrueBlackout;
        public static ConfigEntry<bool> DiversityComputerBegone;
        public static ConfigEntry<bool> OldHalloweenElevatorMusic;
        public static ConfigEntry<string> CentipedeMode;
        public static ConfigEntry<float> CentipedeFixedDamage;
        public static ConfigEntry<int> CentipedeSecondChanceThreshold;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ConsistentRailingCollision = base.Config.Bind("Tweaks", "Consistent Railing Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk (not compatible with Wider Ship Mod).");
            FixedTeleporterButton = base.Config.Bind("Tweaks", "Fixed Teleporter Button", true, "Fixes the teleporter button's hitbox on takeoff or landing by properly parenting it to the ship.");
            BigScrew = base.Config.Bind("Tweaks", "Big Screw", true, "'Big bolt' is accurately renamed to 'Big screw'.");
            DustSpaceClouds = base.Config.Bind("Tweaks", "Dust Space Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (only affects modded content which uses it, since it's never explicitly displayed in vanilla).");
            FallingRotationFix = base.Config.Bind("Tweaks", "Falling Rotation Fix", true, "Normally, if you ever drop an object from really high up, its rotation takes so long to change that it's still rotating when it hits the ground. This tweak properly scales the rotation so objects land normally.");
            OldHalloweenElevatorMusic = base.Config.Bind("Tweaks", "Old Halloween Elevator Music", false, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");
            TrueBlackout = base.Config.Bind("Mod Tweaks", "MrovWeathers True Blackout", false, "EXPERIMENTAL - Blacks out emissive materials during a blackout, so no white spots are leftover from removed lights (does nothing if MrovWeathers isn't installed).");
            DiversityComputerBegone = base.Config.Bind("Mod Tweaks", "Diversity Computer Begone", false, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");
            CentipedeMode = base.Config.Bind("Balance", "Snare Flea Mode", "Vanilla", new ConfigDescription("'Vanilla': Unchanged. - 'Second Chance': Implements the singleplayer 'second chance' mechanic in multiplayer, giving each player a chance to escape once it damages them to low HP. - 'Fixed Damage': Will damage a player for an exact proportion of their maximum health (at the same speed as vanilla).", new AcceptableValueList<string>(["Vanilla","Second Chance","Fixed Damage"])));
            CentipedeFixedDamage = base.Config.Bind("Balance", "Snare Flea Fixed Damage", 0.5f, new ConfigDescription("The proportion of a player's maximum health to take if using the 'Fixed Damage' mode. When set to 50% or above, this effectively gives the player a second chance only if they're above half health (the lower this is set, the more chances).", new AcceptableValueRange<float>(0f, 1f)));
            CentipedeSecondChanceThreshold = base.Config.Bind("Balance", "Snare Flea Second Chance Threshold", 15, new ConfigDescription("At what threshold of health should the snare flea drop off the player if it's using the 'Second Chance' mode (vanilla value in singleplayer is 15 HP).", new AcceptableValueRange<int>(0, 100)));

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            TweaksAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "tweaksassets"));

            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            bool mrovPresent1 = false;
            bool mrovPresent2 = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "MrovWeathers")
                {
                    Logger.LogDebug("Found mrov1!");
                    mrovPresent1 = true;
                }
                else if (assembly.GetName().Name == "WeatherRegistry")
                {
                    Logger.LogDebug("Found mrov2!");
                    mrovPresent2 = true;
                }
                if (mrovPresent1 && mrovPresent2)
                {
                    Logger.LogDebug("Found mrov!");
                    break;
                }
            }

            if (mrovPresent1 && mrovPresent2 && TrueBlackout.Value)
            {
                MrovWeathersPatch.DoPatching();
            }

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
