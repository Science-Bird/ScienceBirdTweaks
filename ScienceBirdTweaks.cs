using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ScienceBirdTweaks.ModPatches;
using ScienceBirdTweaks.Patches;
using UnityEngine;

namespace ScienceBirdTweaks
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MrovWeathers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.TerminalFormatter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("zigzag.SelfSortingStorage", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.WesleyMoons", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.WesleyMoonScripts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.JLLItemModule", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("CodeRebirth", BepInDependency.DependencyFlags.SoftDependency)]

    public class ScienceBirdTweaks : BaseUnityPlugin
    {
        public static ScienceBirdTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle TweaksAssets;

        public static ConfigEntry<bool> ClientsideMode;

        public static ConfigEntry<bool> FixedShipObjects;
        public static ConfigEntry<bool> OnlyFixDefault;
        public static ConfigEntry<bool> AlternateFixLogic;
        public static ConfigEntry<bool> FixedSuitRack;
        public static ConfigEntry<bool> ConsistentCatwalkCollision;
        public static ConfigEntry<bool> TinyTeleporterCollision;
        public static ConfigEntry<bool> BegoneBottomCollision;
        public static ConfigEntry<bool> LargerLeverCollision;
        public static ConfigEntry<bool> FloodlightRotation;
        public static ConfigEntry<bool> FloodlightRotationOnLand;
        public static ConfigEntry<bool> FloodlightPlayerFollow;
        public static ConfigEntry<int> FloodLightIntensity;
        public static ConfigEntry<int> FloodLightAngle;
        public static ConfigEntry<int> FloodLightRange;
        public static ConfigEntry<float> FloodLightRotationSpeed;

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
        public static ConfigEntry<bool> ClientShipItems;
        public static ConfigEntry<bool> LandmineFix;
        public static ConfigEntry<bool> PauseMenuFlickerFix;
        public static ConfigEntry<bool> FallingRotationFix;
        public static ConfigEntry<bool> OldHalloweenElevatorMusic;

        public static ConfigEntry<bool> MineDisableAnimation;
        public static ConfigEntry<bool> SpikeTrapDisableAnimation;
        public static ConfigEntry<string> ZapGunTutorialMode;
        public static ConfigEntry<bool> ZapGunTutorialRevamp;
        public static ConfigEntry<int> ZapGunTutorialCount;
        public static ConfigEntry<bool> ZapGunRework;
        public static ConfigEntry<string> ZapScanPriority;
        public static ConfigEntry<float> ZapGunBattery;
        public static ConfigEntry<bool> ZappableTurrets;
        public static ConfigEntry<float> TurretZapBaseCooldown;
        public static ConfigEntry<bool> ZappableMines;
        public static ConfigEntry<float> MineZapBaseCooldown;
        public static ConfigEntry<bool> ZappableSpikeTraps;
        public static ConfigEntry<float> SpikeTrapBaseCooldown;
        public static ConfigEntry<float> ZapScalingFactor;
        public static ConfigEntry<bool> ZappableBigDoors;
        public static ConfigEntry<bool> PlayerLethalBigDoors;
        public static ConfigEntry<bool> EnemyLethalBigDoors;

        public static ConfigEntry<bool> ShotgunMasterDisable;
        public static ConfigEntry<bool> ShowAmmo;
        public static ConfigEntry<string> SafetyOnString;
        public static ConfigEntry<string> SafetyOffString;
        public static ConfigEntry<bool> UnloadShells;
        public static ConfigEntry<bool> PickUpGunOrbit;
        public static ConfigEntry<bool> PickUpShellsOrbit;
        public static ConfigEntry<bool> ForceRegisterShells;

        public static ConfigEntry<bool> DustSpaceClouds;
        public static ConfigEntry<bool> ThickDustClouds;
        public static ConfigEntry<float> DustCloudsThickness;
        public static ConfigEntry<bool> DustCloudsNoise;

        public static ConfigEntry<bool> PreventWorthlessDespawn;
        public static ConfigEntry<bool> UsePreventDespawnList;
        public static ConfigEntry<string> PreventedDespawnList;
        public static ConfigEntry<bool> ZeroDespawnPreventedItems;
        public static ConfigEntry<string> CustomWorthlessDisplayText;
        public static ConfigEntry<string> WorthlessDisplayTextBlacklist;

        public static ConfigEntry<bool> TrueBlackout;
        public static ConfigEntry<bool> BlackoutOnApparatusRemoval;
        public static ConfigEntry<bool> DisableTrapsOnApparatusRemoval;
        public static ConfigEntry<bool> DisableTrapsOnBreakerSwitch;
        public static ConfigEntry<int> BlackoutFloodLightIntensity;
        public static ConfigEntry<int> BlackoutFloodLightAngle;
        public static ConfigEntry<int> BlackoutFloodLightRange;
        public static ConfigEntry<string> TrueBlackoutNameBlacklist;
        public static ConfigEntry<string> TrueBlackoutHierarchyBlacklist;

        public static ConfigEntry<string> CentipedeMode;
        public static ConfigEntry<float> CentipedeFixedDamage;
        public static ConfigEntry<int> CentipedeSecondChanceThreshold;
        public static ConfigEntry<bool> DropMasks;
        public static ConfigEntry<int> MaskScrapValue;

        public static ConfigEntry<bool> JLLNoisemakerFix;
        public static ConfigEntry<bool> LLLUnlockSyncing;
        public static ConfigEntry<bool> VideoTapeInsertFix;
        public static ConfigEntry<bool> VideoTapeSkip;
        public static ConfigEntry<bool> SSSTerminalStock;
        public static ConfigEntry<bool> DiversityComputerBegone;
        public static ConfigEntry<bool> MrovWeatherTweaksAnnouncement;
        public static ConfigEntry<bool> DebugMode;
        public static ConfigEntry<bool> ExtraLogs;

        public static bool doLobbyCompat = false;
        public static bool mrovPresent1 = false;
        public static bool mrovPresent2 = false;
        public static bool mrovPresent3 = false;
        public static bool mrovPresent4 = false;
        public static bool zigzagPresent = false;
        public static bool wesleyPresent = false;
        public static bool jacobPresent = false;
        public static bool batbyPresent = false;
        public static bool xuPresent = false;

        public static Vector3 ConfigTeleporterSize;

        public static Vector3 ConfigLeverSize;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ClientsideMode = base.Config.Bind("Clientside", "Client-side Mode", false, "EXPERIMENTAL - Enable this if you want to use the mod client-side (i.e. if other players don't have the mod).");
            
            FixedShipObjects = base.Config.Bind("Ship Tweaks", "Fixed Ship Objects", true, "Stops all furniture/unlockable hitboxes from drifting/jittering players on takeoff and landing by properly parenting them to the ship (including teleporter button, welcome mat, etc.).");
            OnlyFixDefault = base.Config.Bind("Ship Tweaks", "Only Fix Vanilla Objects", true, "Only applies the ship object parenting to fix to all the vanilla furniture it's relevant to. You can disable this if you want all furniture to be fixed, but doing so may cause some errors in the console and a bit of lag when loading in.");
            AlternateFixLogic = base.Config.Bind("Ship Tweaks", "Alternate Fix Logic", false, "EXPERIMENTAL - Simplifies parenting fix code. Try this if you're having any unexpected issues with ship objects/furniture (this is automatically used when in client-side mode).");
            FixedSuitRack = base.Config.Bind("Ship Tweaks", "Fixed Suit Rack", true, "Stops suits' hitboxes from drifting on takeoff and landing by properly parenting them to the ship.");
            ConsistentCatwalkCollision = base.Config.Bind("Ship Tweaks", "Consistent Catwalk Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk (not compatible with Wider Ship Mod).");
            TinyTeleporterCollision = base.Config.Bind("Ship Tweaks", "Tiny Teleporter Collision", true, "Shrinks the teleporter and inverse teleporter placement colliders (i.e. just their hitboxes) so they can be put next to all walls and in small nooks of the ship (customizable in Collider Sizes config section).");
            BegoneBottomCollision = base.Config.Bind("Ship Tweaks", "Begone Bottom Collision", false, "Removes collision from components underneath the ship, making it easier to get underneath if you need to (still depending on the moon).");
            LargerLeverCollision = base.Config.Bind("Ship Tweaks", "Larger Lever Collision", false, "Makes the ship's start lever hitbox larger and thus easier to pull (customizable in Collider Sizes config section).");
            FloodlightRotation = base.Config.Bind("Ship Tweaks", "Rotating Floodlight", false, "The ship's top-mounted floodlight can rotate, toggled by a button near the start lever.");
            FloodlightRotationOnLand = base.Config.Bind("Ship Tweaks", "Rotate Floodlight Upon Landing", false, "The ship's floodlight will automatically start rotating when the ship lands.");
            FloodlightPlayerFollow = base.Config.Bind("Ship Tweaks", "Floodlight Follows Players", false, "EXPERIMENTAL - The ship's floodlight will track the closest player (note that since the floodlight point outwards, the targeted player will be between the lights, not actually illuminated).");
            FloodLightRotationSpeed = base.Config.Bind("Ship Tweaks", "Ship Floodlight Rotation Speed", 45.0f, new ConfigDescription("Rotation speed of the ship's floodlights.", new AcceptableValueRange<float>(0.0f, 360.0f)));
            FloodLightIntensity = base.Config.Bind("Ship Tweaks", "Ship Floodlight Intensity", 2275, new ConfigDescription("Lumen value of the ship's floodlights.", new AcceptableValueRange<int>(0, 60000)));
            FloodLightAngle = base.Config.Bind("Ship Tweaks", "Ship Floodlight Angle", 115, new ConfigDescription("Light angle (degrees) of the ship's floodlights.", new AcceptableValueRange<int>(0, 180)));
            FloodLightRange = base.Config.Bind("Ship Tweaks", "Ship Floodlight Range", 45, new ConfigDescription("Light range (meters) of the ship's floodlights.", new AcceptableValueRange<int>(0, 2000)));

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
            ClientShipItems = base.Config.Bind("General Tweaks", "Joining Client Items Fix", true, "When clients join, items aren't normally registered as being inside the ship (meaning you'll see a 'collected' pop-up if you grab them). This fixes that.");
            LandmineFix = base.Config.Bind("General Tweaks", "Fix Mine Explosion On Exit", true, "Fixes a vanilla issue where mines would explode after being stepped on while deactivated then going outside.");
            FallingRotationFix = base.Config.Bind("General Tweaks", "Falling Rotation Fix", false, "Normally, if you ever drop an object from really high up, its rotation takes so long to change that it's still rotating when it hits the ground. This tweak properly scales the rotation so objects land normally.");
            PauseMenuFlickerFix = base.Config.Bind("General Tweaks", "Pause Menu Flicker Fix", false, "'Fixes' the resume button flickering when pausing the game by making the currently selected option always highlighted (will look slightly strange).");
            OldHalloweenElevatorMusic = base.Config.Bind("General Tweaks", "Old Halloween Elevator Music", false, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");

            MineDisableAnimation = base.Config.Bind("Zap Gun & Hazards", "Mine Cooldown Animation", false, "Changes mine lights and sound effects to reflect that it's been disabled (by terminal or otherwise). This is automatically enabled if using the zap gun rework.");
            SpikeTrapDisableAnimation = base.Config.Bind("Zap Gun & Hazards", "Spike Trap Cooldown Animation", false, "Changes spike trap lights to reflect that it's been disabled (by terminal or otherwise). This is automatically enabled if using the zap gun rework.");
            ZapGunTutorialMode = base.Config.Bind("Zap Gun & Hazards", "Zap Gun Tutorial Mode", "Only First Time", new ConfigDescription("'Only First Time': All players will see the tutorial arrow their first few times using the zap gun and never again (I assume this is what's supposed to happen in vanilla). - 'Every Session': All players will see the tutorial arrow the first few times using the zap gun every time they restart the game. - 'Always': All players will always see the tutorial arrow whenever they use the zap gun. - 'Vanilla': Some players (generally the host) always see the tutorial arrow, while others never see it.", new AcceptableValueList<string>(["Only First Time", "Every Session", "Always", "Vanilla"])));
            ZapGunTutorialCount = base.Config.Bind("Zap Gun & Hazards", "Zap Gun Tutorial Count", 2, new ConfigDescription("How many times the tutorial arrow should be displayed (if using 'Only First Time' or 'Every Session' in above config. Vanilla is 2.", new AcceptableValueRange<int>(1, 15)));
            ZapGunTutorialRevamp = base.Config.Bind("Zap Gun & Hazards", "Zap Gun Tutorial Revamp", false, "Changes the mouse graphic in the tutorial to be positioned relative to how much you need to correct the beam (instead of fixed swipes across the arrow).");
            ZapGunRework = base.Config.Bind("Zap Gun & Hazards", "Zap Gun Rework", false, "Activates all of the following config options below, which allow the zap gun to temporarily disable various traps the same way the terminal does (depending on how long you zap them)");
            ZapScanPriority = base.Config.Bind("Zap Gun & Hazards", "Zap Target Priority", "Doors, Enemies, Traps, Players", "Replaces vanilla scan logic to prioritize certain entities in the order specified by this list (if you want to edit the list, use the exact same set of words, not case-sensitive).");
            ZapGunBattery = base.Config.Bind("Zap Gun & Hazards", "Zap Gun Battery", 22f, new ConfigDescription("The battery life of the zap gun (vanilla is 22, pro-flashlight battery is 300 for reference)", new AcceptableValueRange<float>(5f, 150f)));
            ZappableTurrets = base.Config.Bind("Zap Gun & Hazards", "Zappable Turrets", true, "Allows you to disable turrets with the zap gun.");
            TurretZapBaseCooldown = base.Config.Bind("Zap Gun & Hazards", "Zapped Turret Cooldown", 7f, new ConfigDescription("Base cooldown of the turret when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZappableMines = base.Config.Bind("Zap Gun & Hazards", "Zappable Mines", true, "Allows you to disable mines with the zap gun.");
            MineZapBaseCooldown = base.Config.Bind("Zap Gun & Hazards", "Zapped Mine Cooldown", 3.2f, new ConfigDescription("Base cooldown of the mine when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZappableSpikeTraps = base.Config.Bind("Zap Gun & Hazards", "Zappable Spike Traps", true, "Allows you to disable spike traps with the zap gun.");
            SpikeTrapBaseCooldown = base.Config.Bind("Zap Gun & Hazards", "Zapped Spike Trap Cooldown", 7f, new ConfigDescription("Base cooldown of the spike trap when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZapScalingFactor = base.Config.Bind("Zap Gun & Hazards", "Zap Stun Scaling Factor", 0.25f, new ConfigDescription("This is multiplied by the amount of time spent zapping to make the multiplier for the stun time. Decrease this to make stuns shorter, increase this to make them longer", new AcceptableValueRange<float>(0.03f, 3f)));
            ZappableBigDoors = base.Config.Bind("Zap Gun & Hazards", "Zappable Facility Doors", true, "Allows you to hold open the big airlock/pressure doors in the facility interior while zapping them.");
            PlayerLethalBigDoors = base.Config.Bind("Zap Gun & Hazards", "Deadly Facility Doors", true, "Players will be killed by the big facility doors when they close (this usually includes if you try to walk through them while zapping them).");
            EnemyLethalBigDoors = base.Config.Bind("Zap Gun & Hazards", "Facility Doors Deadly To Enemies", true, "Enemies are also killed if they happen to be caught in the facility doors (only if they are normally killable).");

            DustSpaceClouds = base.Config.Bind("Better Dust Clouds", "Dust Space Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (note this weather is unused in vanilla, will only be present with certain modded content).");
            ThickDustClouds = base.Config.Bind("Better Dust Clouds", "Thick Dust Clouds", false, "Makes Dust Clouds visually thicker and more obscuring, in addition to various other internal changes to how the weather is handled, completely replacing vanilla behaviour (note this weather is unused in vanilla, will only be present with certain modded content).");
            DustCloudsThickness = base.Config.Bind("Better Dust Clouds", "Dust Clouds Thickness", 8f, new ConfigDescription("How far you should be able to see in Dust Clouds (lower means thicker clouds). Vanilla value is 17.", new AcceptableValueRange<float>(0.05f, 40f)));
            DustCloudsNoise = base.Config.Bind("Better Dust Clouds", "Dust Clouds Noise", false, "Adds howling wind noise during Dust Clouds weather, the same you hear on blizzard moons like Rend and Dine (note this weather is unused in vanilla, will only be present with certain modded content).");

            PreventWorthlessDespawn = base.Config.Bind("Selective Scrap Keeping", "Keep Worthless Scrap", true, "You won't lose scrap with zero value after your full crew dies.");
            UsePreventDespawnList = base.Config.Bind("Selective Scrap Keeping", "Keep Specific Scrap", false, "You won't lose scrap from the list in the following config option.");
            PreventedDespawnList = base.Config.Bind("Selective Scrap Keeping", "Scrap To Keep", "Shotgun", "Comma separated list of items that should be kept even if everybody dies (e.g. 'Shotgun, Frieren').");
            ZeroDespawnPreventedItems = base.Config.Bind("Selective Scrap Keeping", "Zero Kept Scrap", true, "When a piece of scrap from the prior config list is kept, its scrap value is set to zero.");
            CustomWorthlessDisplayText = base.Config.Bind("Selective Scrap Keeping", "Worthless Display Text", "Value: Priceless", "Custom scan text to display for scrap items with zero value when it's brought back to the ship (set to empty to skip)");
            WorthlessDisplayTextBlacklist = base.Config.Bind("Selective Scrap Keeping", "Worthless Display Text Blacklist", "Shotgun", "Comma separated list of scrap items that will not have their scan text changed.");

            ShotgunMasterDisable = base.Config.Bind("Shotgun QOL", "Master Disable", false, "Reject all changes made by this mod to shotguns, leaving vanilla behaviour untouched (turn this on to disable all shotgun changes).");
            ShowAmmo = base.Config.Bind("Shotgun QOL", "Show Loaded Shells", false, "Shows how many shells your shotgun has left in the top-right tooltips.");
            SafetyOnString = base.Config.Bind("Shotgun QOL", "Shotgun Safety On Tooltip", "The safety is on", "Customize the tooltip for the shotgun safety toggle (vanilla: 'Turn safety off').");
            SafetyOffString = base.Config.Bind("Shotgun QOL", "Shotgun Safety Off Tooltip", "The safety is off", "Customize the tooltip for the shotgun safety toggle (vanilla: 'Turn safety on').");
            UnloadShells = base.Config.Bind("Shotgun QOL", "Unload Shells", false, "Allows you to eject shells already in the shotgun by pressing the reload button (E) while you have no shells to load in your inventory. Top-right tooltips are dynamically adjusted accordingly.");
            PickUpGunOrbit = base.Config.Bind("Shotgun QOL", "Pick Up Gun In Orbit", false, "Allows you to pick up the gun while the ship is in orbit.");
            PickUpShellsOrbit = base.Config.Bind("Shotgun QOL", "Pick Up Shells In Orbit", true, "Allows you to pick up shells while the ship is in orbit (enabled for ease of use with 'Unload Shells').");
            ForceRegisterShells = base.Config.Bind("Shotgun QOL", "Force Register Shells", false, "Troubleshooting option which manually networks shotgun shells so that they should appear on clients. Only enable this if you encounter errors and desyncs when unloading shells. These should normally be networked in vanilla, but certain combinations of mods can interfere with it.");

            BlackoutOnApparatusRemoval = base.Config.Bind("Blackout", "Apparatus True Blackout", false, "Triggers a more comprehensive blackout on apparatus removal, affecting all lights inside and out, along with any emissive materials (does not affects sun).");
            DisableTrapsOnApparatusRemoval = base.Config.Bind("Blackout", "Apparatus Hazard Blackout", false, "Disables all traps/hazards on the map after removing the apparatus.");
            DisableTrapsOnBreakerSwitch = base.Config.Bind("Blackout", "Breaker Hazard Blackout", false, "Also disables all traps/hazards on the map when the breaker power is switched off.");
            TrueBlackout = base.Config.Bind("Blackout", "MrovWeathers True Blackout", true, "Revamps MrovWeathers' blackout so emissive materials are also darkened (no white spots left over), more lights are included, and problematic ones are excluded (like map hazards and outdoor apparatuses).");
            TrueBlackoutNameBlacklist = base.Config.Bind("Blackout", "MrovWeathers True Blackout Name Blacklist", "GunBarrelPos, BulletParticleFlare, LightSphere, Landmine, AnimContainer, BlackoutIgnore, ItemShip, ThrusterContainer", "A blacklist of object names to leave untouched during a blackout. If a light object's parent has the same name as one of these names, it will be skipped. This must be a comma-separated list and is case-sensitive. It is highly recommended you do not remove any of the default values unless you really know what you're doing.");
            TrueBlackoutHierarchyBlacklist = base.Config.Bind("Blackout", "MrovWeathers True Blackout Hierarchy Blacklist", "", "A blacklist of objects to leave untouched during a blackout. If a light object is found anywhere underneath these names in the hierarchy, it will be skipped. This must be a comma-separated list and is case-sensitive. It is recommended to use Name Blacklist whenever possible for performance reasons.");
            BlackoutFloodLightIntensity = base.Config.Bind("Blackout", "Ship Floodlight Intensity in Lumen", 30000, new ConfigDescription("Lumen value of the ship's floodlights during MrovWeathers' blackout, (vanilla is 2275 Lumens). Set to 0 to disable floodlights during blackouts.", new AcceptableValueRange<int>(0, 60000)));
            BlackoutFloodLightAngle = base.Config.Bind("Blackout", "Ship Floodlight Angle in degrees", 80, new ConfigDescription("Light angle (degrees) of the ship's floodlights during MrovWeathers' blackout, (vanilla is 115 degrees).", new AcceptableValueRange<int>(0, 180)));
            BlackoutFloodLightRange = base.Config.Bind("Blackout", "Ship Floodlight Range", 600, new ConfigDescription("Light range (meters) of the ship's floodlights during MrovWeathers' blackout, (vanilla is 44m)", new AcceptableValueRange<int>(0, 2000)));

            CentipedeMode = base.Config.Bind("Gameplay Tweaks", "Snare Flea Mode", "Vanilla", new ConfigDescription("'Vanilla': Unchanged. - 'Second Chance': Implements the singleplayer 'second chance' mechanic in multiplayer, giving each player a chance to escape once it damages them to low HP. - 'Fixed Damage': Will damage a player for an exact proportion of their maximum health (at the same speed as vanilla).", new AcceptableValueList<string>(["Vanilla","Second Chance","Fixed Damage"])));
            CentipedeFixedDamage = base.Config.Bind("Gameplay Tweaks", "Snare Flea Fixed Damage", 0.5f, new ConfigDescription("The proportion of a player's maximum health to take if using the 'Fixed Damage' mode. When set to 50% or above, this effectively gives the player a second chance only if they're above half health (the lower this is set, the more chances).", new AcceptableValueRange<float>(0f, 1f)));
            CentipedeSecondChanceThreshold = base.Config.Bind("Gameplay Tweaks", "Snare Flea Second Chance Threshold", 15, new ConfigDescription("At what threshold of health should the snare flea drop off the player if it's using the 'Second Chance' mode (vanilla value in singleplayer is 15 HP).", new AcceptableValueRange<int>(0, 100)));
            DropMasks = base.Config.Bind("Gameplay Tweaks", "Gimme That Mask", false, "Allows you to grab the masks off of dead masked enemies and sell them (will not work if you have any mod which removes the masks from masked enemies)");
            MaskScrapValue = base.Config.Bind("Gameplay Tweaks", "Dropped Mask Scrap Value", 25, new ConfigDescription("The average scrap value of masks recovered from masked enemies (will vary slightly below and above this).", new AcceptableValueRange<int>(0, 200)));

            JLLNoisemakerFix = base.Config.Bind("Mod Tweaks", "JLL Noisemaker Fix", true, "Fixes an inconsistent issue where JLL spawners wouldn't initialize items correctly, resulting in errors and the item not functioning correctly (for example: Wesley's Moons audio logs not playing when used).");
            LLLUnlockSyncing = base.Config.Bind("Mod Tweaks", "LLL Unlock Syncing", false, "Sends the host's unlocked moons to the clients after they load in, so any moons unlocked by the host will be unlocked by the client as well.");
            VideoTapeInsertFix = base.Config.Bind("Mod Tweaks", "Wesley Moons Tape Insert Fix", false, "EXPERIMENTAL - For Wesley's Moons: attempts to fix an issue where clients are unable to insert cassette tapes into the projector (might also fix issues with registering story log items).");
            VideoTapeSkip = base.Config.Bind("Mod Tweaks", "Wesley Moons Video Tape Skip", false, "For Wesley's Moons: after inserting a casette tape on Galetry, you can interact with the cassette player again to skip the video and unlock the moon immediately.");
            MrovWeatherTweaksAnnouncement = base.Config.Bind("Mod Tweaks", "Weather Tweaks Announcement Change", true, "Makes the wording more clear when a weather change is announced, stating the current weather and the weather it's going to be transitioned into.");
            SSSTerminalStock = base.Config.Bind("Mod Tweaks", "Smart Cupboard Mrov Terminal Stock", true, "If you are using both Self Sorting Storage (which adds the 'smart cupboard') and mrov's TerminalFormatter (which shows a count of items on the ship), items in the cupboard will be counted on the terminal display.");
            DiversityComputerBegone = base.Config.Bind("Mod Tweaks", "Diversity Computer Begone", false, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");
            
            DebugMode = base.Config.Bind("Dev", "Debug Mode", false, "For testing certain interactions and resetting some variables. Do not enable unless you know what you're doing.");
            ExtraLogs = base.Config.Bind("Dev", "Verbose Logs", false, "Extra logging for debugging specific functions.");

            ConfigTeleporterSize = new Vector3(TinyTeleporterSizeX.Value, TinyTeleporterSizeY.Value, TinyTeleporterSizeZ.Value);
            ConfigLeverSize = new Vector3(LargerLeverSizeX.Value, LargerLeverSizeY.Value, LargerLeverSizeZ.Value);

            KeepScrapPatches.Initialize();

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
                else if (assembly.GetName().Name == "WeatherTweaks")
                {
                    Logger.LogDebug("Found mrov4!");
                    mrovPresent4 = true;
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
                else if (assembly.GetName().Name == "CodeRebirth")
                {
                    Logger.LogDebug("Found xu!");
                    xuPresent = true;
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
            if (zigzagPresent && ((mrovPresent3 && SSSTerminalStock.Value) || PreventWorthlessDespawn.Value || UsePreventDespawnList.Value))
            {
                SSSPatches.DoPatching();
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

            NetcodePatcher();

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
            if (ClientsideMode.Value) { return; }// skip all netcode patches in client-side mode
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    // skipping un-patched methods
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
