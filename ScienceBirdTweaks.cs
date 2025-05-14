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
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]// ScienceBird.ScienceBirdTweaks, ScienceBirdTweaks
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MrovWeathers", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("WeatherTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TerminalFormatter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("zigzag.SelfSortingStorage", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.WesleyMoons", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.WesleyMoonScripts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("JacobG5.JLLItemModule", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TestAccount666.ShipWindowsBeta", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TestAccount666.GoodItemScan", BepInDependency.DependencyFlags.SoftDependency)]

    public class ScienceBirdTweaks : BaseUnityPlugin
    {
        public static ScienceBirdTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle TweaksAssets;

        public static ConfigEntry<bool> FixedShipObjects, OnlyFixDefault, AlternateFixLogic, FixedSuitRack, ConsistentCatwalkCollision, TinyTeleporterCollision, BegoneBottomCollision, LargerLeverCollision;
        public static ConfigEntry<string> ModdedListMode, ModdedUnlockableList;

        public static ConfigEntry<bool> RemoveClipboard, RemoveStickyNote, RemoveTeleporterCord, RemoveLongTube, RemoveGenerator, RemoveHelmet, RemoveOxygenTanks, RemoveBoots, RemoveAirFilter, RemoveBatteries, RemoveCruiserClipboard;

        public static ConfigEntry<float> TinyTeleporterSizeX, TinyTeleporterSizeY, TinyTeleporterSizeZ, LargerLeverSizeX, LargerLeverSizeY, LargerLeverSizeZ;

        public static ConfigEntry<bool> FloodlightRotation, FloodlightRotationOnLand, FloodlightExtraControls, FloodlightPlayerFollow;
        public static ConfigEntry<int> FloodLightIntensity, FloodLightAngle, FloodLightRange;
        public static ConfigEntry<float> FloodLightRotationSpeed;
        public static ConfigEntry<bool> FancyPanel;
        public static ConfigEntry<bool> DynamicOccupancySign, OccupancyScribble;
        public static ConfigEntry<string> OccupancyFixedValue;
        public static ConfigEntry<bool> PlayGlobalDeathSFX, AutoTeleportBody, UnrecoverableNotification;
        public static ConfigEntry<string> Red1Tip, Red2Tip, Black1Tip, Black2Tip, Knob1Tip, Knob2Tip, Knob3Tip, SmallKnobTip, SmallRedTip, SmallGreenTip;

        public static ConfigEntry<bool> BigScrew, MissingHoverTipFix, SmokeFix, BridgeItemsFix, CleanBeltBagUI, ClientShipItems, LandmineFix, CrouchDamageAnimation, PauseMenuFlickerFix, FallingRotationFix, OldHalloweenElevatorMusic;
        public static ConfigEntry<string> StartingMoon, MuteScrapList;

        public static ConfigEntry<bool> CoilheadElevatorFix;
        public static ConfigEntry<bool> DropMasks;
        public static ConfigEntry<int> MaskScrapValue;
        public static ConfigEntry<string> CentipedeMode;
        public static ConfigEntry<float> CentipedeFixedDamage;
        public static ConfigEntry<int> CentipedeSecondChanceThreshold;
        public static ConfigEntry<bool> LeviathanSurfacePatch, LeviathanQuicksand;
        public static ConfigEntry<string> LeviathanNaturalSurfaces;
        public static ConfigEntry<bool> ManeaterTransformInterrupt, ManeaterFastDoors;
        public static ConfigEntry<bool> TulipSnakeMuteLaugh;

        public static ConfigEntry<bool> MineDisableAnimation, SpikeTrapDisableAnimation, ZapGunTutorialRevamp, ZapGunRework, ZappableTurrets, ZappableMines, ZappableSpikeTraps, ZappableBigDoors, PlayerLethalBigDoors, EnemyLethalBigDoors;
        public static ConfigEntry<string> ZapGunTutorialMode, ZapScanPriority;
        public static ConfigEntry<int> ZapGunTutorialCount;
        public static ConfigEntry<float> ZapGunBattery, TurretZapBaseCooldown, MineZapBaseCooldown, SpikeTrapBaseCooldown, ZapScalingFactor;

        public static ConfigEntry<bool> ShotgunMasterDisable, ShowAmmo, UnloadShells, DoAmmoCheck, PickUpGunOrbit, PickUpShellsOrbit;
        public static ConfigEntry<string> SafetyOnString, SafetyOffString;

        public static ConfigEntry<bool> DustSpaceClouds, ThickDustClouds, DustCloudsNoise;
        public static ConfigEntry<float> DustCloudsThickness;

        public static ConfigEntry<bool> PreventWorthlessDespawn, UsePreventDespawnList, ZeroDespawnPreventedItems;
        public static ConfigEntry<string> PreventedDespawnList, CustomWorthlessDisplayText, WorthlessDisplayTextBlacklist;

        public static ConfigEntry<bool> TrueBlackout, BlackoutOnApparatusRemoval, DisableTrapsOnApparatusRemoval, DisableTrapsOnBreakerSwitch, BlackoutSFX, BlacklistLightAnimators, BlackoutOnlySun, BlacklistPoles, BlacklistEmergency;
        public static ConfigEntry<int> BlackoutFloodLightIntensity, BlackoutFloodLightAngle, BlackoutFloodLightRange;
        public static ConfigEntry<string> TrueBlackoutNameBlacklist, TrueBlackoutHierarchyBlacklist;

        public static ConfigEntry<bool> JLLNoisemakerFix, LLLUnlockSyncing, LLLShipLeverFix, VideoTapeInsertFix, VideoTapeSkip, ShipWindowsShutterFix, SSSTerminalStock, DiversityComputerBegone, MrovWeatherTweaksAnnouncement;
        public static ConfigEntry<bool> ClientsideMode, DebugMode, ExtraLogs, DisableWarnings, InteriorLogging;

        public static bool doLobbyCompat = false;
        public static bool mrovPresent1 = false;
        public static bool mrovPresent2 = false;
        public static bool mrovPresent3 = false;
        public static bool mrovPresent4 = false;
        public static bool zigzagPresent = false;
        public static bool wesleyPresent = false;
        public static bool jacobPresent = false;
        public static bool batbyPresent = false;
        //public static bool xuPresent = false;
        public static bool test1Present = false;
        public static bool test2Present = false;

        public static Vector3 ConfigTeleporterSize;

        public static Vector3 ConfigLeverSize;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            FixedShipObjects = base.Config.Bind("1 i. Ship Tweaks", "Fixed Ship Objects", true, "Stops all furniture/unlockable hitboxes from drifting/jittering players on takeoff and landing by properly parenting them to the ship (including teleporter button, welcome mat, etc.).");
            OnlyFixDefault = base.Config.Bind("1 i. Ship Tweaks", "Only Fix Vanilla Objects", true, "Only applies the ship object parenting to fix to all the vanilla furniture it's relevant to. You can disable this if you want all furniture to be fixed, but doing so may cause some errors in the console and a bit of lag when loading in.");
            ModdedListMode = base.Config.Bind("1 i. Ship Tweaks", "Fix Modded List Mode", "Don't Use List", new ConfigDescription("Choose whether the list of modded furniture below will be a whitelist (only listed items will be fixed), or a blacklist (everything EXCEPT listed items will be fixed).", new AcceptableValueList<string>(["Don't Use List", "Whitelist", "Blacklist"])));
            ModdedUnlockableList = base.Config.Bind("1 i. Ship Tweaks", "Fix Modded Objects List", "", "List the exact names of modded furniture items you want to either blacklist or whitelist (not case-sensitive).");
            AlternateFixLogic = base.Config.Bind("1 i. Ship Tweaks", "Alternate Fix Logic", false, "EXPERIMENTAL - Simplifies parenting fix code. Try this if you're having any unexpected issues with ship objects/furniture (this is automatically used when in client-side mode).");
            FixedSuitRack = base.Config.Bind("1 i. Ship Tweaks", "Fixed Suit Rack", true, "Stops suits' hitboxes from drifting on takeoff and landing by properly parenting them to the ship.");
            ConsistentCatwalkCollision = base.Config.Bind("1 i. Ship Tweaks", "Consistent Catwalk Collision", true, "Ship catwalk has consistent collision outside its railing, so you can always jump and stand on the edge of the catwalk (not compatible with Wider Ship Mod).");
            TinyTeleporterCollision = base.Config.Bind("1 i. Ship Tweaks", "Tiny Teleporter Collision", true, "Shrinks the teleporter and inverse teleporter placement colliders (i.e. just their hitboxes) so they can be put next to all walls and in small nooks of the ship (customizable in Collider Sizes config section).");
            BegoneBottomCollision = base.Config.Bind("1 i. Ship Tweaks", "Begone Bottom Collision", false, "Removes collision from components underneath the ship, making it easier to get underneath if you need to (still depending on the moon).");
            LargerLeverCollision = base.Config.Bind("1 i. Ship Tweaks", "Larger Lever Collision", false, "Makes the ship's start lever hitbox larger and thus easier to pull (customizable in Collider Sizes config section).");

            TinyTeleporterSizeX = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Tiny Teleporter Size X", 1.5f, "Vanilla: 2.48");
            TinyTeleporterSizeY = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Tiny Teleporter Size Y", 4f, "(Height) Vanilla: 6");
            TinyTeleporterSizeZ = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Tiny Teleporter Size Z", 1.6f, "Vanilla: 2.6");
            LargerLeverSizeX = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Larger Lever Size X", 1.25f, "(Length, e.g. from lever to monitor screen) Vanilla: 1");
            LargerLeverSizeY = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Larger Lever Size Y", 1.75f, "(Height) Vanilla: 1");
            LargerLeverSizeZ = base.Config.Bind("1 ii. Ship Tweaks Collider Sizes", "Larger Lever Size Z", 1.65f, "(Width, e.g. left to right edge of monitor) Vanilla: 1");

            RemoveClipboard = base.Config.Bind("1 iii. Ship Tweaks Removals", "Clipboard", false, "Removes the service manual clipboard.");
            RemoveStickyNote = base.Config.Bind("1 iii. Ship Tweaks Removals", "Sticky Note", false, "Removes the 'ACCESS FILE: SIGURD' hint sticky note.");
            RemoveTeleporterCord = base.Config.Bind("1 iii. Ship Tweaks Removals", "Teleporter Cord", false, "Removes the cord trailing off the teleporter button (which won't connect to the teleporter if you move it).");
            RemoveLongTube = base.Config.Bind("1 iii. Ship Tweaks Removals", "Long Tube", false, "Removes the long tube trailing off the generator and across the floor of the ship.");
            RemoveGenerator = base.Config.Bind("1 iii. Ship Tweaks Removals", "Generator", false, "Removes the generator next to the door.");
            RemoveHelmet = base.Config.Bind("1 iii. Ship Tweaks Removals", "Helmet", false, "Removes the helmet on the counter by the main monitor.");
            RemoveOxygenTanks = base.Config.Bind("1 iii. Ship Tweaks Removals", "Oxygen Tanks", false, "Removes the oxygen tanks leaning against the wall.");
            RemoveBoots = base.Config.Bind("1 iii. Ship Tweaks Removals", "Boots", false, "Removes the boots by the suit rack.");
            RemoveAirFilter = base.Config.Bind("1 iii. Ship Tweaks Removals", "Air Filter", false, "Removes the air filter in the corner by the monitors.");
            RemoveBatteries = base.Config.Bind("1 iii. Ship Tweaks Removals", "Batteries", false, "Removes the batteries strewn across the desk by the monitors.");
            RemoveCruiserClipboard = base.Config.Bind("1 iii. Ship Tweaks Removals", "Cruiser Clipboard", false, "Removes the clipboard manual which comes with the Company Cruiser.");

            FloodlightRotation = base.Config.Bind("2. Ship Additions", "Rotating Floodlight", false, "The ship's top-mounted floodlight can rotate, toggled by a button near the start lever.");
            FloodLightRotationSpeed = base.Config.Bind("2. Ship Additions", "Ship Floodlight Rotation Speed", 45.0f, new ConfigDescription("Rotation speed of the ship's floodlights.", new AcceptableValueRange<float>(0.0f, 360.0f)));
            FloodLightIntensity = base.Config.Bind("2. Ship Additions", "Ship Floodlight Intensity", 2275, new ConfigDescription("Lumen value of the ship's floodlights.", new AcceptableValueRange<int>(0, 60000)));
            FloodLightAngle = base.Config.Bind("2. Ship Additions", "Ship Floodlight Angle", 115, new ConfigDescription("Light angle (degrees) of the ship's floodlights.", new AcceptableValueRange<int>(0, 180)));
            FloodLightRange = base.Config.Bind("2. Ship Additions", "Ship Floodlight Range", 45, new ConfigDescription("Light range (meters) of the ship's floodlights.", new AcceptableValueRange<int>(0, 2000)));
            FloodlightRotationOnLand = base.Config.Bind("2. Ship Additions", "Rotate Floodlight Upon Landing", false, "The ship's floodlight will automatically start rotating when the ship lands.");
            FloodlightExtraControls = base.Config.Bind("2. Ship Additions", "Floodlight Configuration Controls", true, "If floodlight rotation is enabled, the ship's main panel will come with additional controls to dynamically adjust the floodlight's speed, reset its position, or switch to targeting the closest player.");
            FloodlightPlayerFollow = base.Config.Bind("2. Ship Additions", "Floodlight Follows Players Button", false, "EXPERIMENTAL - If extra controls is enabled, a button is added to track the closest player (note that since the floodlight points outwards, the targeted player will be between the lights, not actually illuminated).");
            FancyPanel = base.Config.Bind("2. Ship Additions", "Fancy Button Panel", false, "Revamps the ship's main panel to have interactable buttons and knobs, with working lights that will activate at certain times. None of these buttons do anything (yet), unless you have the rotating floodlight and its extra configuration enabled, which gives a purpose to a few of the buttons.");
            DynamicOccupancySign = base.Config.Bind("2. Ship Additions", "Dynamic Occupancy Sign", false, "The ship's 'maximum occupancy' sign will now update accordingly if more than 4 players join the lobby.");
            OccupancyScribble = base.Config.Bind("2. Ship Additions", "Occupancy Sign Scribble", false, "Instead of just changing the number on the sign, enabling this will scribble out the old number and replace it with a new hand-drawn one, giving it a more amateur feel.");
            OccupancyFixedValue = base.Config.Bind("2. Ship Additions", "Occupancy Sign Fixed Value", "None", new ConfigDescription("Pick a maximum occupancy here if you'd rather a single fixed value rather than one which updates dynamically.", new AcceptableValueList<string>(["None", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "Infinite"])));
            PlayGlobalDeathSFX = base.Config.Bind("2. Ship Additions", "Broadcast Death Notification", false, "All players will recieve a quick succession of beeps to let them know that somebody has just died.");
            AutoTeleportBody = base.Config.Bind("2. Ship Additions", "Auto Teleport Bodies", false, "If the ship has a teleporter, it will automatically teleport a player's body back upon their death, letting all players know via the scrap collected notification. If the player's body has been destroyed, an alternative notification will appear instead.");
            UnrecoverableNotification = base.Config.Bind("2. Ship Additions", "Unrecoverable Body Notification", false, "Whenever a player is teleported but their body cannot be recovered, an alternative notification to the 'body collected' scrap notification will appear. This happens when the above auto teleport feature cannot recover a body, but enabling it here will make it happen on player-initiated teleports as well, and even if the above feature is disabled.");
            Red1Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Bottom Right Red", "", "If using fancy button panel, the tooltip for the bottom right red button (overriden by rotating floodlight).");
            Red2Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Middle Right Red", "", "If using fancy button panel, the tooltip for the smaller middle right red button.");
            Black1Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Bottom Middle Black", "", "If using fancy button panel, the tooltip for the bottom middle black button (overriden by floodlight configuration controls).");
            Black2Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Middle Right Black", "", "If using fancy button panel, the tooltip for the middle right black button (overriden by floodlight configuration controls).");
            Knob1Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Middle Right Knob", "", "If using fancy button panel, the tooltip for the middle right knob (overriden by floodlight configuration controls).");
            Knob2Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Top Middle Knob 1", "", "If using fancy button panel, the tooltip for the top middle knob closest to the player.");
            Knob3Tip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Top Middle Knob 2", "", "If using fancy button panel, the tooltip for the top middle knob farther from the player.");
            SmallKnobTip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Small Knob", "", "If using fancy button panel, the tooltip for the small knob at the top middle.");
            SmallRedTip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Small Red", "", "If using fancy button panel, the tooltip for the small red button on the right.");
            SmallGreenTip = base.Config.Bind("2. Ship Additions", "Fancy Panel Tooltips III Small Green", "", "If using fancy button panel, the tooltip for the small green button on the right.");

            BigScrew = base.Config.Bind("3. General Tweaks", "Big Screw", true, "'Big bolt' is accurately renamed to 'Big screw'.");
            MissingHoverTipFix = base.Config.Bind("3. General Tweaks", "Missing Hover Tip Fix", true, "When starting a hold interaction before actually hovering over the interaction trigger, the hover tip will still display.");
            SmokeFix = base.Config.Bind("3. General Tweaks", "Smoke Particle Fix", true, "The exhaust smoke from the cruiser and smoke trail from old birds now blend normally into fog so the particles don't stand out so much from far away.");
            BridgeItemsFix = base.Config.Bind("3. General Tweaks", "Bridge Items Fix", true, "When a (vanilla) bridge collapses, any items left on it will now fall as well, rather than floating.");
            CleanBeltBagUI = base.Config.Bind("3. General Tweaks", "Clean Belt Bag UI", true, "Scan nodes currently on screen are cleared when opening the belt bag's inventory to reduce visual clutter.");
            ClientShipItems = base.Config.Bind("3. General Tweaks", "Joining Client Items Fix", true, "When clients join, items aren't normally registered as being inside the ship (meaning you'll see a 'collected' pop-up if you grab them). This fixes that.");
            LandmineFix = base.Config.Bind("3. General Tweaks", "Fix Mine Explosion On Exit", true, "Fixes a vanilla issue where mines would explode after being stepped on while deactivated then going outside.");
            CrouchDamageAnimation = base.Config.Bind("3. General Tweaks", "No Crouched Damage Animation", true, "Disables the player damaged animation if they are crouching, as this animation would cause players to stand up when damaged.");
            StartingMoon = base.Config.Bind("3. General Tweaks", "Starting Moon", "Experimentation", "LLL REQUIRED - The default moon your ship will orbit when creating a new save or after being fired (only the moon name needs to be included, not the number). This can be either a vanilla or modded moon.");
            MuteScrapList = base.Config.Bind("3. General Tweaks", "Muted Scrap List", "", "A comma separated list of items which will have their passive noise effects disabled (e.g. 'Comedy, Tragedy, Clock, Toy robot, Radioactive barrel', not case-sensitive). Eyeless dogs will also no longer hear these sounds.");
            FallingRotationFix = base.Config.Bind("3. General Tweaks", "Falling Rotation Fix", false, "Normally, if you ever drop an object from really high up, its rotation takes so long to change that it's still rotating when it hits the ground. This tweak properly scales the rotation so objects land normally.");
            PauseMenuFlickerFix = base.Config.Bind("3. General Tweaks", "Pause Menu Flicker Fix", false, "'Fixes' the resume button flickering when pausing the game by making the currently selected option always highlighted (will look slightly strange).");
            OldHalloweenElevatorMusic = base.Config.Bind("3. General Tweaks", "Old Halloween Elevator Music", false, "Restores mineshaft elevator to its old Halloween behaviour, playing a random selection of groovy tracks (disabled if ButteryStancakes' HalloweenElevator is installed).");

            CoilheadElevatorFix = base.Config.Bind("4. Enemy Tweaks", "Coilhead Elevator Fix", true, "Fixes inconsistent sightlines when standing in an elevator, which could cause coilheads to kill players in the elevator while being looked at.");
            DropMasks = base.Config.Bind("4. Enemy Tweaks", "Gimme That Mask", false, "Allows you to grab the masks off of dead masked enemies and sell them (will not work if you have any mod which removes the masks from masked enemies)");
            MaskScrapValue = base.Config.Bind("4. Enemy Tweaks", "Dropped Mask Scrap Value", 25, new ConfigDescription("The average scrap value of masks recovered from masked enemies (will vary slightly below and above this).", new AcceptableValueRange<int>(0, 200)));
            CentipedeMode = base.Config.Bind("4. Enemy Tweaks", "Snare Flea Mode", "Vanilla", new ConfigDescription("'Vanilla': Unchanged. - 'Second Chance': Implements the singleplayer 'second chance' mechanic in multiplayer, giving each player a chance to escape once it damages them to low HP. - 'Fixed Damage': Will damage a player for an exact proportion of their maximum health (at the same speed as vanilla).", new AcceptableValueList<string>(["Vanilla", "Second Chance", "Fixed Damage"])));
            CentipedeFixedDamage = base.Config.Bind("4. Enemy Tweaks", "Snare Flea Fixed Damage", 0.5f, new ConfigDescription("The proportion of a player's maximum health to take if using the 'Fixed Damage' mode. When set to 50% or above, this effectively gives the player a second chance only if they're above half health (the lower this is set, the more chances).", new AcceptableValueRange<float>(0f, 1f)));
            CentipedeSecondChanceThreshold = base.Config.Bind("4. Enemy Tweaks", "Snare Flea Second Chance Threshold", 15, new ConfigDescription("At what threshold of health should the snare flea drops off the player if it's using the 'Second Chance' mode (vanilla value in singleplayer is 15 HP).", new AcceptableValueRange<int>(0, 100)));
            LeviathanSurfacePatch = base.Config.Bind("4. Enemy Tweaks", "Earth Leviathan More Surfaces", false, "Allows the earth leviathan to burrow through and emerge from more types of surfaces, using the list below.");
            LeviathanNaturalSurfaces = base.Config.Bind("4. Enemy Tweaks", "Earth Leviathan Surfaces List", "Wood, Tiles, Aluminum, Rock, Catwalk, Concrete, Metal, Carpet, Puddle, Untagged", "Surface tags (tied to footstep sounds) earth leviathans should be able to burrow through if above option is enabled (in addition to the default tags Grass, Gravel, and Snow)");
            LeviathanQuicksand = base.Config.Bind("4. Enemy Tweaks", "Earth Leviathan Quicksand", false, "Earth leviathans will leave behind patches of quicksand where they emerge and enter the ground (note that quicksand cannot appear on every type of surface).");
            ManeaterTransformInterrupt = base.Config.Bind("4. Enemy Tweaks", "Maneater Transformation Interrupt", false, "Hitting the maneater while it's transforming will cause it to immediately exit its state and become active.");
            ManeaterFastDoors = base.Config.Bind("4. Enemy Tweaks", "Maneater Fast Doors", false, "The maneater will move more quickly through doors.");
            TulipSnakeMuteLaugh = base.Config.Bind("4. Enemy Tweaks", "Quiet Tulip Snakes", false, "Tulip snake chuckles will no longer alert eyeless dogs.");

            MineDisableAnimation = base.Config.Bind("5. Zap Gun & Hazards", "Mine Cooldown Animation", false, "Changes mine lights and sound effects to reflect that it's been disabled (by terminal or otherwise). This is automatically enabled if using the zap gun rework.");
            SpikeTrapDisableAnimation = base.Config.Bind("5. Zap Gun & Hazards", "Spike Trap Cooldown Animation", false, "Changes spike trap lights to reflect that it's been disabled (by terminal or otherwise). This is automatically enabled if using the zap gun rework.");
            ZapGunTutorialMode = base.Config.Bind("5. Zap Gun & Hazards", "Zap Gun Tutorial Mode", "Only First Time", new ConfigDescription("'Only First Time': All players will see the tutorial arrow their first few times using the zap gun and never again (I assume this is what's supposed to happen in vanilla). - 'Every Session': All players will see the tutorial arrow the first few times using the zap gun every time they restart the game. - 'Always': All players will always see the tutorial arrow whenever they use the zap gun. - 'Vanilla': Some players (generally the host) always see the tutorial arrow, while others never see it.", new AcceptableValueList<string>(["Only First Time", "Every Session", "Always", "Vanilla"])));
            ZapGunTutorialCount = base.Config.Bind("5. Zap Gun & Hazards", "Zap Gun Tutorial Count", 2, new ConfigDescription("How many times the tutorial arrow should be displayed (if using 'Only First Time' or 'Every Session' in above config. Vanilla is 2.", new AcceptableValueRange<int>(1, 15)));
            ZapGunTutorialRevamp = base.Config.Bind("5. Zap Gun & Hazards", "Zap Gun Tutorial Revamp", false, "Changes the mouse graphic in the tutorial to be positioned relative to how much you need to correct the beam (instead of fixed swipes across the arrow).");
            ZapGunRework = base.Config.Bind("5. Zap Gun & Hazards", "Zap Gun Rework", false, "Activates all of the following config options below, which allow the zap gun to temporarily disable various traps the same way the terminal does (depending on how long you zap them)");
            ZapScanPriority = base.Config.Bind("5. Zap Gun & Hazards", "Zap Target Priority", "Doors, Enemies, Traps, Players", "Replaces vanilla scan logic to prioritize certain entities in the order specified by this list (if you want to edit the list, use the exact same set of words, not case-sensitive).");
            ZapGunBattery = base.Config.Bind("5. Zap Gun & Hazards", "Zap Gun Battery", 22f, new ConfigDescription("The battery life of the zap gun (vanilla is 22, pro-flashlight battery is 300 for reference)", new AcceptableValueRange<float>(5f, 150f)));
            ZappableTurrets = base.Config.Bind("5. Zap Gun & Hazards", "Zappable Turrets", true, "Allows you to disable turrets with the zap gun.");
            TurretZapBaseCooldown = base.Config.Bind("5. Zap Gun & Hazards", "Zapped Turret Cooldown", 7f, new ConfigDescription("Base cooldown of the turret when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZappableMines = base.Config.Bind("5. Zap Gun & Hazards", "Zappable Mines", true, "Allows you to disable mines with the zap gun.");
            MineZapBaseCooldown = base.Config.Bind("5. Zap Gun & Hazards", "Zapped Mine Cooldown", 3.2f, new ConfigDescription("Base cooldown of the mine when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZappableSpikeTraps = base.Config.Bind("5. Zap Gun & Hazards", "Zappable Spike Traps", true, "Allows you to disable spike traps with the zap gun.");
            SpikeTrapBaseCooldown = base.Config.Bind("5. Zap Gun & Hazards", "Zapped Spike Trap Cooldown", 7f, new ConfigDescription("Base cooldown of the spike trap when zapped (will be more or less than this depending how long it's zapped for). Default value is the vanilla value for being disabled by the terminal.", new AcceptableValueRange<float>(1f, 50f)));
            ZapScalingFactor = base.Config.Bind("5. Zap Gun & Hazards", "Zap Stun Scaling Factor", 0.25f, new ConfigDescription("This is multiplied by the amount of time spent zapping to make the multiplier for the stun time. Decrease this to make stuns shorter, increase this to make them longer", new AcceptableValueRange<float>(0.03f, 3f)));
            ZappableBigDoors = base.Config.Bind("5. Zap Gun & Hazards", "Zappable Facility Doors", true, "Allows you to hold open the big airlock/pressure doors in the facility interior while zapping them.");
            PlayerLethalBigDoors = base.Config.Bind("5. Zap Gun & Hazards", "Deadly Facility Doors", true, "Players will be killed by the big facility doors when they close (this usually includes if you try to walk through them while zapping them).");
            EnemyLethalBigDoors = base.Config.Bind("5. Zap Gun & Hazards", "Facility Doors Deadly To Enemies", true, "Enemies are also killed if they happen to be caught in the facility doors (only if they are normally killable).");

            DustSpaceClouds = base.Config.Bind("6. Better Dust Clouds", "Dust Space Clouds", true, "Adds a space to the 'DustClouds' weather whenever it's displayed, making it 'Dust Clouds' (note this weather is unused in vanilla, will only be present with certain modded content).");
            ThickDustClouds = base.Config.Bind("6. Better Dust Clouds", "Thick Dust Clouds", false, "Makes Dust Clouds visually thicker and more obscuring, in addition to various other internal changes to how the weather is handled, completely replacing vanilla behaviour (note this weather is unused in vanilla, will only be present with certain modded content).");
            DustCloudsThickness = base.Config.Bind("6. Better Dust Clouds", "Dust Clouds Thickness", 8f, new ConfigDescription("How far you should be able to see in Dust Clouds (lower means thicker clouds). Vanilla value is 17.", new AcceptableValueRange<float>(0.05f, 40f)));
            DustCloudsNoise = base.Config.Bind("6. Better Dust Clouds", "Dust Clouds Noise", false, "Adds howling wind noise during Dust Clouds weather, the same you hear on blizzard moons like Rend and Dine (note this weather is unused in vanilla, will only be present with certain modded content).");

            PreventWorthlessDespawn = base.Config.Bind("7. Selective Scrap Keeping", "Keep Worthless Scrap", true, "You won't lose scrap with zero value after your full crew dies.");
            UsePreventDespawnList = base.Config.Bind("7. Selective Scrap Keeping", "Keep Specific Scrap", false, "You won't lose scrap from the list in the following config option.");
            PreventedDespawnList = base.Config.Bind("7. Selective Scrap Keeping", "Scrap To Keep", "Shotgun", "Comma separated list of items that should be kept even if everybody dies (e.g. 'Shotgun, Frieren').");
            ZeroDespawnPreventedItems = base.Config.Bind("7. Selective Scrap Keeping", "Zero Kept Scrap", true, "When a piece of scrap from the prior config list is kept, its scrap value is set to zero.");
            CustomWorthlessDisplayText = base.Config.Bind("7. Selective Scrap Keeping", "Worthless Display Text", "Value: Priceless", "Custom scan text to display for scrap items with zero value when it's brought back to the ship (set to empty to skip)");
            WorthlessDisplayTextBlacklist = base.Config.Bind("7. Selective Scrap Keeping", "Worthless Display Text Blacklist", "Shotgun", "Comma separated list of scrap items that will not have their scan text changed.");

            ShotgunMasterDisable = base.Config.Bind("8. Shotgun QOL", "Master Disable", false, "Reject all changes made by this mod to shotguns, leaving vanilla behaviour untouched (turn this on to disable all shotgun changes).");
            ShowAmmo = base.Config.Bind("8. Shotgun QOL", "Show Loaded Shells", false, "Shows how many shells your shotgun has left in the top-right tooltips.");
            SafetyOnString = base.Config.Bind("8. Shotgun QOL", "Shotgun Safety On Tooltip", "The safety is on", "Customize the tooltip for the shotgun safety toggle (vanilla: 'Turn safety off').");
            SafetyOffString = base.Config.Bind("8. Shotgun QOL", "Shotgun Safety Off Tooltip", "The safety is off", "Customize the tooltip for the shotgun safety toggle (vanilla: 'Turn safety on').");
            UnloadShells = base.Config.Bind("8. Shotgun QOL", "Unload Shells", false, "Allows you to eject shells already in the shotgun by holding the reload button (E) while you have no shells to load in your inventory. Top-right tooltips are dynamically adjusted accordingly.");
            DoAmmoCheck = base.Config.Bind("8. Shotgun QOL", "Check and Unload Animation", false, "When pressing reload while unable to reload, the shotgun will open to reveal how many shells it has. Enabling this will also mean the eject shells function (which has you holding down the reload button) will be animated.");
            PickUpGunOrbit = base.Config.Bind("8. Shotgun QOL", "Pick Up Gun In Orbit", false, "Allows you to pick up the gun while the ship is in orbit.");
            PickUpShellsOrbit = base.Config.Bind("8. Shotgun QOL", "Pick Up Shells In Orbit", true, "Allows you to pick up shells while the ship is in orbit (enabled for ease of use with 'Unload Shells').");
            //ForceRegisterShells = base.Config.Bind("8. Shotgun QOL", "Force Register Shells", false, "Troubleshooting option which manually networks shotgun shells so that they should appear on clients. Only enable this if you encounter errors and desyncs when unloading shells. These should normally be networked in vanilla, but certain combinations of mods can interfere with it.");

            BlackoutOnApparatusRemoval = base.Config.Bind("9. Blackout", "Apparatus True Blackout", false, "Triggers a more comprehensive blackout on apparatus removal, affecting all lights inside and out, along with any emissive materials (does not affects sun).");
            DisableTrapsOnApparatusRemoval = base.Config.Bind("9. Blackout", "Apparatus Hazard Blackout", false, "Disables all traps/hazards on the map after removing the apparatus.");
            DisableTrapsOnBreakerSwitch = base.Config.Bind("9. Blackout", "Breaker Hazard Blackout", false, "Also disables all traps/hazards on the map when the breaker power is switched off.");
            TrueBlackout = base.Config.Bind("9. Blackout", "MrovWeathers True Blackout", true, "Revamps MrovWeathers' blackout so emissive materials are also darkened (no white spots left over), more lights are included, and problematic ones are excluded (like map hazards and outdoor apparatuses).");
            BlackoutSFX = base.Config.Bind("9. Blackout", "Blackout Sound Effect", true, "Plays a global sound effect when a blackout of any kind occurs.");
            TrueBlackoutNameBlacklist = base.Config.Bind("9. Blackout", "MrovWeathers True Blackout Name Blacklist", "GunBarrelPos, BulletParticleFlare, LightSphere, Landmine, AnimContainer, BlackoutIgnore, ItemShip, ThrusterContainer", "A blacklist of object names to leave untouched during a blackout. If a light object's parent has the same name as one of these names, it will be skipped. This must be a comma-separated list and is case-sensitive. It is highly recommended you do not remove any of the default values unless you really know what you're doing.");
            TrueBlackoutHierarchyBlacklist = base.Config.Bind("9. Blackout", "MrovWeathers True Blackout Hierarchy Blacklist", "", "A blacklist of objects to leave untouched during a blackout. If a light object is found anywhere underneath these names in the hierarchy, it will be skipped. This must be a comma-separated list and is case-sensitive. It is recommended to use Name Blacklist whenever possible for performance reasons."); 
            BlacklistLightAnimators = base.Config.Bind("9. Blackout", "Ignore Animators", false, "Exclude any lights associated with animations (will not exclude any manually included animators, such as the vanilla light switches). With mods like Facility Meltdown (or any mod/moon which animates lights), this will allow the lights to animate as usual rather than being blacked out.");
            BlacklistPoles = base.Config.Bind("9. Blackout", "Ignore Guidance Poles", false, "Exclude the guidance pole lights found on moons like Rend, Dine, and Titan.");
            BlacklistEmergency = base.Config.Bind("9. Blackout", "Ignore Emergency Exit Lights", false, "Exclude the red lights mounted on interior emergency exits.");
            BlackoutOnlySun = base.Config.Bind("9. Blackout", "Only Blackout Sun", false, "The blackout weather will only blackout the sun and no other lights.");
            BlackoutFloodLightIntensity = base.Config.Bind("9. Blackout", "Ship Floodlight Intensity in Lumen", 30000, new ConfigDescription("Lumen value of the ship's floodlights during MrovWeathers' blackout, (vanilla is 2275 Lumens). Set to 0 to disable floodlights during blackouts.", new AcceptableValueRange<int>(0, 60000)));
            BlackoutFloodLightAngle = base.Config.Bind("9. Blackout", "Ship Floodlight Angle in degrees", 80, new ConfigDescription("Light angle (degrees) of the ship's floodlights during MrovWeathers' blackout, (vanilla is 115 degrees).", new AcceptableValueRange<int>(0, 180)));
            BlackoutFloodLightRange = base.Config.Bind("9. Blackout", "Ship Floodlight Range", 600, new ConfigDescription("Light range (meters) of the ship's floodlights during MrovWeathers' blackout, (vanilla is 44m)", new AcceptableValueRange<int>(0, 2000)));

            JLLNoisemakerFix = base.Config.Bind("A. Mod Tweaks", "JLL Noisemaker Fix", true, "Fixes an inconsistent issue where JLL spawners wouldn't initialize items correctly, resulting in errors and the item not functioning correctly (for example: Wesley's Moons audio logs not playing when used).");
            LLLUnlockSyncing = base.Config.Bind("A. Mod Tweaks", "LLL Unlock Syncing", false, "Sends the host's unlocked moons to the clients after they load in, so any moons unlocked by the host will be unlocked by the client as well.");
            LLLShipLeverFix = base.Config.Bind("A. Mod Tweaks", "LLL Ship Lever Fix", true, "Fixes the ship lever remaining interactable while routing.");
            VideoTapeInsertFix = base.Config.Bind("A. Mod Tweaks", "Wesley Moons Tape Insert Fix", false, "EXPERIMENTAL - For Wesley's Moons: attempts to fix an issue where clients are unable to insert cassette tapes into the projector (might also fix issues with registering story log items).");
            VideoTapeSkip = base.Config.Bind("A. Mod Tweaks", "Wesley Moons Video Tape Skip", false, "For Wesley's Moons: after inserting a casette tape on Galetry, you can interact with the cassette player again to skip the video and unlock the moon immediately.");
            ShipWindowsShutterFix = base.Config.Bind("A. Mod Tweaks", "ShipWindowsBeta Shutter Fix", true, "Closes the ship window shutters when taking off from a planet (requires you to have 'Hide Moon Transitions' enabled in ShipWindowsBeta config).");
            MrovWeatherTweaksAnnouncement = base.Config.Bind("A. Mod Tweaks", "Weather Tweaks Announcement Change", true, "Makes the wording more clear when a weather change is announced, stating the current weather and the weather it's going to be transitioned into.");
            SSSTerminalStock = base.Config.Bind("A. Mod Tweaks", "Smart Cupboard Mrov Terminal Stock", true, "If you are using both Self Sorting Storage (which adds the 'smart cupboard') and mrov's TerminalFormatter (which shows a count of items on the ship), items in the cupboard will be counted on the terminal display.");
            DiversityComputerBegone = base.Config.Bind("A. Mod Tweaks", "Diversity Computer Begone", false, "Removes the floppy reader computer from Diversity and any floppy disks that spawn (does nothing if Diversity isn't installed).");

            ClientsideMode = base.Config.Bind("C. Technical", "Client-side Mode", false, "EXPERIMENTAL - Enable this if you want to use the mod client-side (i.e. if other players don't have the mod).");
            DebugMode = base.Config.Bind("C. Technical", "Dev Mode", false, "For testing certain interactions and resetting some variables. Do not enable unless you know what you're doing.");
            ExtraLogs = base.Config.Bind("C. Technical", "Verbose Logs", false, "Extra logging for debugging specific functions.");
            DisableWarnings = base.Config.Bind("C. Technical", "Disable Warning Popups", false, "Disables all red warning boxes and their sound effects, vanilla and modded (e.g. when attempting to land with 0 days left).");
            InteriorLogging = base.Config.Bind("C. Technical", "Interior Analysis", false, "LLL REQUIRED - Logs the area and scrap density of the generated interior and its tiles.");

            ConfigTeleporterSize = new Vector3(TinyTeleporterSizeX.Value, TinyTeleporterSizeY.Value, TinyTeleporterSizeZ.Value);
            ConfigLeverSize = new Vector3(LargerLeverSizeX.Value, LargerLeverSizeY.Value, LargerLeverSizeZ.Value);

            KeepScrapPatches.Initialize();
            
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            TweaksAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "tweaksassets"));

            BlackoutTriggerPatches.LoadAssets();

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
                //else if (assembly.GetName().Name == "CodeRebirth")
                //{
                //    Logger.LogDebug("Found xu!");
                //    xuPresent = true;
                //}
                else if (assembly.GetName().Name == "TestAccount666.ShipWindowsBeta")
                {
                    Logger.LogDebug("Found test1!");
                    test1Present = true;
                }
                else if (assembly.GetName().Name == "TestAccount666.GoodItemScan")
                {
                    Logger.LogDebug("Found test2!");
                    test2Present = true;
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
            if (batbyPresent)
            {
                LLLPatches.DoPatching();
            }
            if (jacobPresent && JLLNoisemakerFix.Value)
            {
                JLLPatches.DoPatching();
            }
            if (test1Present && ShipWindowsShutterFix.Value)
            {
                ShipWindowsPatch.DoPatching();
            }

            if (DynamicOccupancySign.Value || OccupancyFixedValue.Value != "None")
            {
                OccupancyPatch.LoadAssets();
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
                    if ((method.Name.Contains("CheckUnlocksClientRpc") || method.Name.Contains("ConfigLoader")) && !batbyPresent)
                    {
                        continue;
                    }
                    if ((method.Name.Contains("CollectDataServerRpc") || method.Name.Contains("SendDataClientRpc") || method.Name.Contains("ResetDictClientRpc")) && !zigzagPresent)
                    {
                        continue;
                    }
                    if ((method.Name.Contains("StopTapeServerRpc") || method.Name.Contains("StopTapeClientRpc")) && !wesleyPresent)
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
