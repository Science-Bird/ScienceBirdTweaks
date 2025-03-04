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
        public static ConfigEntry<bool> ConsistentCatwalkCollision;
        public static ConfigEntry<bool> TinyTeleporterCollision;
        public static ConfigEntry<bool> BegoneBottomCollision;
        public static ConfigEntry<bool> LargerLeverCollision;
        public static ConfigEntry<bool> FixedTeleporterButton;
        public static ConfigEntry<bool> FixedSuitRack;
        public static ConfigEntry<float> TinyTeleporterSizeX;
        public static ConfigEntry<float> TinyTeleporterSizeY;
        public static ConfigEntry<float> TinyTeleporterSizeZ;
        public static ConfigEntry<float> LargerLeverSizeX;
        public static ConfigEntry<float> LargerLeverSizeY;
        public static ConfigEntry<float> LargerLeverSizeZ;
        public static ConfigEntry<bool> BigScrew;
        public static ConfigEntry<bool> DustSpaceClouds;
        public static ConfigEntry<bool> FallingRotationFix;
        public static ConfigEntry<bool> TrueBlackout;
        public static ConfigEntry<bool> DiversityComputerBegone;
        public static ConfigEntry<bool> OldHalloweenElevatorMusic;
        public static ConfigEntry<string> CentipedeMode;
        public static ConfigEntry<float> CentipedeFixedDamage;
        public static ConfigEntry<int> CentipedeSecondChanceThreshold;

        public static Vector3 ConfigTeleporterSize;

        public static Vector3 ConfigLeverSize;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ConsistentCatwalkCollision = base.Config.Bind("Ship Tweaks", "Consistent Catwalk Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk (not compatible with Wider Ship Mod).");
            TinyTeleporterCollision = base.Config.Bind("Ship Tweaks", "Tiny Teleporter Collision", true, "Shrinks the teleporter and inverse teleporter placement colliders (i.e. just their hitboxes) so they can be put next to all walls and in small nooks of the ship (customizable in Collider Sizes config section).");
            BegoneBottomCollision = base.Config.Bind("Ship Tweaks", "Begone Bottom Collision", false, "Removes collision from components underneath the ship, making it easier to get underneath if you need to (still depending on the moon).");
            LargerLeverCollision = base.Config.Bind("Ship Tweaks", "Larger Lever Collision", false, "Makes the ship's start lever hitbox larger and thus easier to pull (customizable in Collider Sizes config section).");
            FixedTeleporterButton = base.Config.Bind("Ship Tweaks", "Fixed Teleporter Button", true, "Fixes the teleporter button's hitbox on takeoff or landing by properly parenting it to the ship.");
            FixedSuitRack = base.Config.Bind("Ship Tweaks", "Fixed Suit Rack", true, "Fixes suits' hitbox on takeoff or landing by properly parenting them to the ship.");
            TinyTeleporterSizeX = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size X", 1.5f, "Vanilla: 2.48");
            TinyTeleporterSizeY = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size Y", 4f, "(Height) Vanilla: 6");
            TinyTeleporterSizeZ = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size Z", 1.6f, "Vanilla: 2.6");
            LargerLeverSizeX = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size X", 1.25f, "(Length, e.g. from lever to monitor screen) Vanilla: 1");
            LargerLeverSizeY = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size Y", 1.75f, "(Height) Vanilla: 1");
            LargerLeverSizeZ = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size Z", 1.65f, "(Width, e.g. left to right edge of monitor) Vanilla: 1");
            BigScrew = base.Config.Bind("General Tweaks", "Big Screw", true, "'Big bolt' is accurately renamed to 'Big screw'.");
            DustSpaceClouds = base.Config.Bind("General Tweaks", "Dust Space Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (only affects modded content which uses it, since it's never explicitly displayed in vanilla).");
            FallingRotationFix = base.Config.Bind("General Tweaks", "Falling Rotation Fix", true, "Normally, if you ever drop an object from really high up, its rotation takes so long to change that it's still rotating when it hits the ground. This tweak properly scales the rotation so objects land normally.");
            OldHalloweenElevatorMusic = base.Config.Bind("General Tweaks", "Old Halloween Elevator Music", false, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");
            TrueBlackout = base.Config.Bind("Mod Tweaks", "MrovWeathers True Blackout", false, "EXPERIMENTAL - Blacks out emissive materials during a blackout, so no white spots are leftover from removed lights (does nothing if MrovWeathers isn't installed).");
            DiversityComputerBegone = base.Config.Bind("Mod Tweaks", "Diversity Computer Begone", false, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");
            CentipedeMode = base.Config.Bind("Gameplay Tweaks", "Snare Flea Mode", "Vanilla", new ConfigDescription("'Vanilla': Unchanged. - 'Second Chance': Implements the singleplayer 'second chance' mechanic in multiplayer, giving each player a chance to escape once it damages them to low HP. - 'Fixed Damage': Will damage a player for an exact proportion of their maximum health (at the same speed as vanilla).", new AcceptableValueList<string>(["Vanilla","Second Chance","Fixed Damage"])));
            CentipedeFixedDamage = base.Config.Bind("Gameplay Tweaks", "Snare Flea Fixed Damage", 0.5f, new ConfigDescription("The proportion of a player's maximum health to take if using the 'Fixed Damage' mode. When set to 50% or above, this effectively gives the player a second chance only if they're above half health (the lower this is set, the more chances).", new AcceptableValueRange<float>(0f, 1f)));
            CentipedeSecondChanceThreshold = base.Config.Bind("Gameplay Tweaks", "Snare Flea Second Chance Threshold", 15, new ConfigDescription("At what threshold of health should the snare flea drop off the player if it's using the 'Second Chance' mode (vanilla value in singleplayer is 15 HP).", new AcceptableValueRange<int>(0, 100)));

            ConfigTeleporterSize = new Vector3(TinyTeleporterSizeX.Value, TinyTeleporterSizeY.Value, TinyTeleporterSizeZ.Value);
            ConfigLeverSize = new Vector3(LargerLeverSizeX.Value, LargerLeverSizeY.Value, LargerLeverSizeZ.Value);

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
