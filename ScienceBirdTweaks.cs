using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ScienceBirdTweaks.ModPatches;
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
        public static ConfigEntry<bool> FixedShipObjects;
        public static ConfigEntry<bool> OnlyFixDefault;
        public static ConfigEntry<bool> FixedSuitRack;
        public static ConfigEntry<bool> RemoveClipboard;
        public static ConfigEntry<bool> RemoveStickyNote;
        public static ConfigEntry<bool> RemoveTeleporterCord;
        public static ConfigEntry<bool> RemoveLongTube;
        public static ConfigEntry<bool> RemoveGenerator;
        public static ConfigEntry<bool> RemoveHelmet;
        public static ConfigEntry<bool> RemoveOxygenTanks;
        public static ConfigEntry<bool> RemoveBoots;
        public static ConfigEntry<bool> RemoveAirFilter;
        public static ConfigEntry<bool> RemoveBatteries;
        public static ConfigEntry<float> TinyTeleporterSizeX;
        public static ConfigEntry<float> TinyTeleporterSizeY;
        public static ConfigEntry<float> TinyTeleporterSizeZ;
        public static ConfigEntry<float> LargerLeverSizeX;
        public static ConfigEntry<float> LargerLeverSizeY;
        public static ConfigEntry<float> LargerLeverSizeZ;
        public static ConfigEntry<bool> BigScrew;
        public static ConfigEntry<bool> FallingRotationFix;
        public static ConfigEntry<bool> OldHalloweenElevatorMusic;
        public static ConfigEntry<bool> DustSpaceClouds;
        public static ConfigEntry<bool> ThickDustClouds;
        public static ConfigEntry<float> DustCloudsThickness;
        public static ConfigEntry<bool> DustCloudsNoise;
        public static ConfigEntry<bool> SSSTerminalStock;
        public static ConfigEntry<bool> JLLNoisemakerFix;
        public static ConfigEntry<bool> LLLUnlockSyncing;
        public static ConfigEntry<bool> VideoTapeInsertFix;
        public static ConfigEntry<bool> VideoTapeSkip;
        public static ConfigEntry<bool> TrueBlackout;
        public static ConfigEntry<bool> DiversityComputerBegone;
        public static ConfigEntry<string> CentipedeMode;
        public static ConfigEntry<float> CentipedeFixedDamage;
        public static ConfigEntry<int> CentipedeSecondChanceThreshold;
        public static ConfigEntry<bool> DebugMode;

        public static bool doLobbyCompat = false;
        public static bool mrovPresent1 = false;
        public static bool mrovPresent2 = false;
        public static bool mrovPresent3 = false;
        public static bool zigzagPresent = false;
        public static bool wesleyPresent = false;
        public static bool jacobPresent = false;
        public static bool batbyPresent = false;

        public static Vector3 ConfigTeleporterSize;

        public static Vector3 ConfigLeverSize;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            FixedShipObjects = base.Config.Bind("Ship Tweaks", "Fixed Ship Objects", true, "Stops all furniture/unlockable hitboxes from drifting/jittering players on takeoff and landing by properly parenting them to the ship (including teleporter button, welcome mat, etc.).");
            OnlyFixDefault = base.Config.Bind("Ship Tweaks", "Only Fix Vanilla Objects", true, "Only applies the ship object parenting to fix to all the vanilla furniture it's relevant to. You can disable this if you want all furniture to be fixed, but doing so may cause some errors in the console and a bit of lag when loading in.");
            FixedSuitRack = base.Config.Bind("Ship Tweaks", "Fixed Suit Rack", true, "Stops suits' hitboxes from drifting on takeoff and landing by properly parenting them to the ship.");
            ConsistentCatwalkCollision = base.Config.Bind("Ship Tweaks", "Consistent Catwalk Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk (not compatible with Wider Ship Mod).");
            TinyTeleporterCollision = base.Config.Bind("Ship Tweaks", "Tiny Teleporter Collision", true, "Shrinks the teleporter and inverse teleporter placement colliders (i.e. just their hitboxes) so they can be put next to all walls and in small nooks of the ship (customizable in Collider Sizes config section).");
            BegoneBottomCollision = base.Config.Bind("Ship Tweaks", "Begone Bottom Collision", false, "Removes collision from components underneath the ship, making it easier to get underneath if you need to (still depending on the moon).");
            LargerLeverCollision = base.Config.Bind("Ship Tweaks", "Larger Lever Collision", false, "Makes the ship's start lever hitbox larger and thus easier to pull (customizable in Collider Sizes config section).");
            RemoveClipboard = base.Config.Bind("Ship Tweaks Removals", "Clipboard", false, "Removes the service manual clipboard.");
            RemoveStickyNote = base.Config.Bind("Ship Tweaks Removals", "Sticky Note", false, "Removes the 'ACCESS FILE: SIGURD' hint sticky note.");
            RemoveTeleporterCord = base.Config.Bind("Ship Tweaks Removals", "Teleporter Cord", false, "Removes the cord trailing off the teleporter button (which won't connect to the teleporter if you move it).");
            RemoveLongTube = base.Config.Bind("Ship Tweaks Removals", "Long Tube", false, "Removes the long tube trailing off the generator and across the floor of the ship.");
            RemoveGenerator = base.Config.Bind("Ship Tweaks Removals", "Generator", false, "Removes the generator next to the door.");
            RemoveHelmet = base.Config.Bind("Ship Tweaks Removals", "Helmet", false, "Removes the helmet on the counter by the main monitor.");
            RemoveOxygenTanks = base.Config.Bind("Ship Tweaks Removals", "Oxygen Tanks", false, "Removes the oxygen tanks leaning against the wall.");
            RemoveBoots = base.Config.Bind("Ship Tweaks Removals", "Boots", false, "Removes the boots by the suit rack.");
            RemoveAirFilter = base.Config.Bind("Ship Tweaks Removals", "Air Filter", false, "Removes the air filter in the corner by the monitors.");
            RemoveBatteries = base.Config.Bind("Ship Tweaks Removals", "Batteries", false, "Removes the batteries strewn across the desk by the monitors.");
            TinyTeleporterSizeX = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size X", 1.5f, "Vanilla: 2.48");
            TinyTeleporterSizeY = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size Y", 4f, "(Height) Vanilla: 6");
            TinyTeleporterSizeZ = base.Config.Bind("Ship Tweaks Collider Sizes", "Tiny Teleporter Size Z", 1.6f, "Vanilla: 2.6");
            LargerLeverSizeX = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size X", 1.25f, "(Length, e.g. from lever to monitor screen) Vanilla: 1");
            LargerLeverSizeY = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size Y", 1.75f, "(Height) Vanilla: 1");
            LargerLeverSizeZ = base.Config.Bind("Ship Tweaks Collider Sizes", "Larger Lever Size Z", 1.65f, "(Width, e.g. left to right edge of monitor) Vanilla: 1");
            BigScrew = base.Config.Bind("General Tweaks", "Big Screw", true, "'Big bolt' is accurately renamed to 'Big screw'.");
            FallingRotationFix = base.Config.Bind("General Tweaks", "Falling Rotation Fix", true, "Normally, if you ever drop an object from really high up, its rotation takes so long to change that it's still rotating when it hits the ground. This tweak properly scales the rotation so objects land normally.");
            OldHalloweenElevatorMusic = base.Config.Bind("General Tweaks", "Old Halloween Elevator Music", false, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");
            DustSpaceClouds = base.Config.Bind("Better Dust Clouds", "Dust Space Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (note this weather is unused in vanilla, will only be present with certain modded content).");
            ThickDustClouds = base.Config.Bind("Better Dust Clouds", "Thick Dust Clouds", false, "Makes Dust Clouds visually thicker and more obscuring, in addition to various other internal changes to how the weather is handled, completely replacing vanilla behaviour (note this weather is unused in vanilla, will only be present with certain modded content).");
            DustCloudsThickness = base.Config.Bind("Better Dust Clouds", "Dust Clouds Thickness", 8f, new ConfigDescription("How far you should be able to see in Dust Clouds (lower means thicker clouds). Vanilla value is 17.", new AcceptableValueRange<float>(0.05f, 40f)));
            DustCloudsNoise = base.Config.Bind("Better Dust Clouds", "Dust Clouds Noise", false, "Adds howling wind noise during Dust Clouds weather, the same you hear on blizzard moons like Rend and Dine (note this weather is unused in vanilla, will only be present with certain modded content).");
            JLLNoisemakerFix = base.Config.Bind("Mod Tweaks", "JLL Noisemaker Fix", true, "Fixes an inconsistent issue where JLL spawners wouldn't initialize items correctly, resulting in errors and the item not functioning correctly (for example: Wesley's Moons audio logs not playing when used).");
            LLLUnlockSyncing = base.Config.Bind("Mod Tweaks", "LLL Unlock Syncing", false, "Sends the host's unlocked moons to the clients after they load in, so any moons unlocked by the host will be unlocked by the client as well.");
            VideoTapeInsertFix = base.Config.Bind("Mod Tweaks", "Wesley Moons Tape Insert Fix", false, "EXPERIMENTAL - For Wesley's Moons: attempts to fix an issue where clients are unable to insert cassette tapes into the projector (might also fix issues with registering story log items).");
            VideoTapeSkip = base.Config.Bind("Mod Tweaks", "Wesley Moons Video Tape Skip", false, "For Wesley's Moons: after inserting a casette tape on Galetry, you can interact with the cassette player again to skip the video and unlock the moon immediately.");
            SSSTerminalStock = base.Config.Bind("Mod Tweaks", "Smart Cupboard Mrov Terminal Stock", true, "If you are using both Self Sorting Storage (which adds the 'smart cupboard') and mrov's TerminalFormatter (which shows a count of items on the ship), items in the cupboard will be counted on the terminal display.");
            TrueBlackout = base.Config.Bind("Mod Tweaks", "MrovWeathers True Blackout", false, "EXPERIMENTAL - Blacks out emissive materials during a blackout, so no white spots are leftover from removed lights (does nothing if MrovWeathers isn't installed).");
            DiversityComputerBegone = base.Config.Bind("Mod Tweaks", "Diversity Computer Begone", false, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");
            CentipedeMode = base.Config.Bind("Gameplay Tweaks", "Snare Flea Mode", "Vanilla", new ConfigDescription("'Vanilla': Unchanged. - 'Second Chance': Implements the singleplayer 'second chance' mechanic in multiplayer, giving each player a chance to escape once it damages them to low HP. - 'Fixed Damage': Will damage a player for an exact proportion of their maximum health (at the same speed as vanilla).", new AcceptableValueList<string>(["Vanilla","Second Chance","Fixed Damage"])));
            CentipedeFixedDamage = base.Config.Bind("Gameplay Tweaks", "Snare Flea Fixed Damage", 0.5f, new ConfigDescription("The proportion of a player's maximum health to take if using the 'Fixed Damage' mode. When set to 50% or above, this effectively gives the player a second chance only if they're above half health (the lower this is set, the more chances).", new AcceptableValueRange<float>(0f, 1f)));
            CentipedeSecondChanceThreshold = base.Config.Bind("Gameplay Tweaks", "Snare Flea Second Chance Threshold", 15, new ConfigDescription("At what threshold of health should the snare flea drop off the player if it's using the 'Second Chance' mode (vanilla value in singleplayer is 15 HP).", new AcceptableValueRange<int>(0, 100)));
            DebugMode = base.Config.Bind("Dev", "Debug Mode", false, "For testing certain interactions and resetting some variables. Do not enable unless you know what you're doing.");

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

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "BMX.LobbyCompatibility")
                {
                    Logger.LogDebug("Found BMX!");
                    doLobbyCompat = true;
                }
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
                else if (assembly.GetName().Name == "TerminalFormatter")
                {
                    Logger.LogDebug("Found mrov3!");
                    mrovPresent3 = true;
                }
                else if (assembly.GetName().Name == "SelfSortingStorage")
                {
                    Logger.LogDebug("Found zigzag!");
                    zigzagPresent = true;
                }
                else if (assembly.GetName().Name == "WesleyMoons")
                {
                    Logger.LogDebug("Found wesley!");
                    wesleyPresent = true;
                }
                else if (assembly.GetName().Name == "JLLItemsModule")
                {
                    Logger.LogDebug("Found jacob!");
                    jacobPresent = true;
                }
                else if (assembly.GetName().Name == "LethalLevelLoader")
                {
                    Logger.LogDebug("Found batby!");
                    batbyPresent = true;
                }
            }

            if (doLobbyCompat)
            {
                LobbyCompatibility.RegisterCompatibility();
            }

            if (mrovPresent1 && mrovPresent2 && TrueBlackout.Value)
            {
                MrovWeathersPatch.DoPatching();
            }
            if (zigzagPresent && mrovPresent3 && SSSTerminalStock.Value)
            {
                SSSPatch.DoPatching();
            }
            if (wesleyPresent && (VideoTapeSkip.Value || VideoTapeInsertFix.Value))
            {
                WesleyPatches.DoPatching();
            }
            if (batbyPresent && LLLUnlockSyncing.Value)
            {
                LLLPatches.DoPatching();
            }
            if (jacobPresent && JLLNoisemakerFix.Value)
            {
                JLLPatches.DoPatching();
            }

            Harmony.PatchAll();

            NetcodePatcher(); // ONLY RUN ONCE

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name.Contains("CheckUnlocksClientRpc") && !batbyPresent)
                    {
                        continue;
                    }
                    if (method.Name.Contains("CollectDataServerRpc") && !zigzagPresent)
                    {
                        continue;
                    }
                    if (method.Name.Contains("SendDataClientRpc") && !zigzagPresent)
                    {
                        continue;
                    }
                    if (method.Name.Contains("ResetDictClientRpc") && !zigzagPresent)
                    {
                        continue;
                    }
                    if (method.Name.Contains("StopTapeServerRpc") && !wesleyPresent)
                    {
                        continue;
                    }
                    if (method.Name.Contains("StopTapeClientRpc") && !wesleyPresent)
                    {
                        continue;
                    }
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
