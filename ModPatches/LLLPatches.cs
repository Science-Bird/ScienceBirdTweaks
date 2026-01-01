using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using ScienceBirdTweaks.Scripts;
using DunGen;
using LethalLevelLoader;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace ScienceBirdTweaks.ModPatches
{
    public class LLLPatches
    {
        public static void DoPatching()
        {
            if (ScienceBirdTweaks.LLLUnlockSyncing.Value)
            {
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(GameNetworkManager), "Start"), postfix: new HarmonyMethod(typeof(LLLSyncPatch).GetMethod("InitializeSyncPrefab")));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc)), postfix: new HarmonyMethod(typeof(LLLSyncPatch).GetMethod("OnLateClientSync")));
            }

            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(DungeonGenerator), "Generate"), prefix: new HarmonyMethod(typeof(InteriorPatches).GetMethod("OnGeneration")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(RoundManager), "SpawnScrapInLevel"), prefix: new HarmonyMethod(typeof(InteriorPatches).GetMethod("BeforeScrapSpawn")), postfix: new HarmonyMethod(typeof(InteriorPatches).GetMethod("AfterScrapSpawn")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(RoundManager), "GetRandomNavMeshPositionInBoxPredictable"), postfix: new HarmonyMethod(typeof(InteriorPatches).GetMethod("NavBoxPatch")));

            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "Start"), prefix: new HarmonyMethod(typeof(InteriorConfigPatch).GetMethod("OnStart"), after: ["imabatby.lethallevelloader"]));

            if (ScienceBirdTweaks.LLLShipLeverFix.Value)
            {
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "LateUpdate"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnUpdate"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ChangeLevel"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnChangeLevel"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ArriveAtLevel"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnArrive"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ShipLeave"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnLeave"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "SetShipReadyToLand"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("ShipReady"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartMatchLever), "PullLeverAnim"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnAnim"), after: ["imabatby.lethallevelloader"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "SceneManager_OnLoadComplete1"), postfix: new HarmonyMethod(typeof(LeverPatch).GetMethod("OnLoad"), after: ["imabatby.lethallevelloader"]));
            }
        }
    }

    public class LeverPatch
    {
        public static StartMatchLever storedLever;
        public static bool disabled = false;
        public static void OnUpdate()
        {
            if (disabled && storedLever != null)
            {
                storedLever.triggerScript.interactable = false;
            }
        }

        public static void SetLeverInteractable(bool enabled)
        {
            if (storedLever == null)
            {
                storedLever = Object.FindObjectOfType<StartMatchLever>();
            }
            if (storedLever != null)
            {
                disabled = !enabled;
            }
        }

        public static void OnChangeLevel(StartOfRound __instance)
        {
            if (__instance.travellingToNewLevel)
            {
                SetLeverInteractable(false);
            }
        }

        public static void OnArrive()
        {
            SetLeverInteractable(true);
        }

        public static void OnLeave()
        {
            SetLeverInteractable(false);
        }

        public static void ShipReady()
        {
            SetLeverInteractable(true);
        }

        public static void OnAnim()
        {
            SetLeverInteractable(false);
        }

        public static void OnLoad()
        {
            SetLeverInteractable(true);
        }
    }

    public class InteriorConfigPatch
    {
        private static Dictionary<ExtendedDungeonFlow, (string, string)> dungeonDict = new Dictionary<ExtendedDungeonFlow, (string modname, string flowname)>();
        public static Dictionary<ExtendedDungeonFlow, ConfigEntry<int>> configDict = new Dictionary<ExtendedDungeonFlow, ConfigEntry<int>>();
        public static bool enabled = false;

        public static void OnStart(StartOfRound __instance)
        {
            foreach (var extendedFlow in PatchedContent.ExtendedDungeonFlows)// dungeon dictionary exists to associate certain dungeons with their names and respective mods (which is used to write config entries)
            {
                dungeonDict.TryAdd(extendedFlow, (extendedFlow.ModName, extendedFlow.DungeonName.Replace("\n","").Replace("\t","").Replace("\\","").Replace("\"","").Replace("'","").Replace("[","").Replace("]","")));
            }
            ConfigLoader();
        }

        static void ConfigLoader()
        {
            foreach (var dungeon in dungeonDict)// the dungeon dict is used to bind the config entries in the config dict (still binding config entries the same way as usual, just abstracted by dictionary)
            {
                configDict.TryAdd(dungeon.Key, ScienceBirdTweaks.Instance.Config.Bind("Y. Interior Scrap Bonus", $"{dungeon.Value.Item2}", 0, new ConfigDescription("This number of extra scrap items will be spawned whenever this interior generates.", new AcceptableValueRange<int>(0, 30))));
            }
            if (configDict.Any(x => x.Value.Value != 0))// if any of the scrap counts are non-zero
            {
                enabled = true;
            }
        }
    }

    public class LLLSyncPatch
    {
        public static GameObject syncPrefab;

        public static void InitializeSyncPrefab(GameNetworkManager __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Initializing sync object!");
            syncPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("LLLSyncScript");
            NetworkManager.Singleton.AddNetworkPrefab(syncPrefab);
        }

        public static void OnLateClientSync(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                GameObject syncObj = GameObject.Find("LLLSyncScript(Clone)");
                if (syncObj == null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Creating sync object since none exist...");
                    syncObj = Object.Instantiate(syncPrefab, Vector3.zero, Quaternion.identity);
                    syncObj.GetComponent<NetworkObject>().Spawn();
                }
                LLLUnlockSync syncScript = syncObj.GetComponent<LLLUnlockSync>();
                syncScript.CheckUnlocks();// everything done by attached network behaviour
            }
        }
    }

    public class InteriorPatches
    {
        public static int? savedMin;
        public static int? savedMax;
        public static bool inScrapSpawnContext = false;
        public static List<Vector3> spawnPositions;

        public static void OnGeneration(DungeonGenerator __instance)
        {
            if (!InteriorConfigPatch.enabled || StartOfRound.Instance.currentLevel.PlanetName == "754 Conflux") { return; }

            if (InteriorConfigPatch.configDict.TryGetValue(DungeonManager.CurrentExtendedDungeonFlow, out var value))
            {
                if (value.Value != 0)// if config val set, save the level's original min and max, then add scrap number to both (equivalent to adding that number of bonus scrap to the interior)
                {
                    ScienceBirdTweaks.Logger.LogInfo($"Adding {value.Value} additional scrap to interior!");
                    savedMin = StartOfRound.Instance.currentLevel.minScrap;
                    savedMax = StartOfRound.Instance.currentLevel.maxScrap;
                    StartOfRound.Instance.currentLevel.minScrap += value.Value;
                    StartOfRound.Instance.currentLevel.maxScrap += value.Value;
                }
            }
        }

        public static void BeforeScrapSpawn(RoundManager __instance)
        {
            if (StartOfRound.Instance.currentLevel.PlanetName == "754 Conflux") { return; }
            inScrapSpawnContext = true;
            if (ScienceBirdTweaks.InteriorLogging.Value)
            {
                spawnPositions = new List<Vector3>();
            }
        }

        public static void NavBoxPatch(RoundManager __instance, Vector3 pos, float radius)
        {
            if (StartOfRound.Instance.currentLevel.PlanetName == "754 Conflux") { return; }
            if (inScrapSpawnContext && ScienceBirdTweaks.InteriorLogging.Value)// the function patched here is what distributes scrap spawns in an area around some point. the position passed to it is the position of the spawner doing the spawns
            {
                spawnPositions.Add(pos);// we save the position of the spawner so we can use it to find the spawner after
            }
        }

        public static void AfterScrapSpawn(RoundManager __instance)
        {
            if (StartOfRound.Instance.currentLevel.PlanetName == "754 Conflux") { return; }
            inScrapSpawnContext = false;
            if (InteriorConfigPatch.enabled)
            {
                if (savedMin != null && savedMax != null)// restore original min max
                {
                    ScienceBirdTweaks.Logger.LogDebug("Restoring values!");
                    StartOfRound.Instance.currentLevel.minScrap = (int)savedMin;
                    StartOfRound.Instance.currentLevel.maxScrap = (int)savedMax;
                    savedMin = null;
                    savedMax = null;
                }
            }

            if (!ScienceBirdTweaks.InteriorLogging.Value) { return; }

            List<Tile> tiles = new List<Tile>(UnityEngine.Object.FindObjectsOfType<Tile>().ToList());
            Dictionary<Tile, int> tileDict = tiles.ToDictionary(x => x, x => 0);// find all tiles, we're going to count how much scrap is in each using this dictionary

            RandomScrapSpawn[] scrapSpawns = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
            List<RandomScrapSpawn> usedSpawns = new List<RandomScrapSpawn>();
            for (int i = 0; i < scrapSpawns.Length; i++)
            {
                if (scrapSpawns[i].spawnUsed || spawnPositions.Contains(scrapSpawns[i].transform.position))// items spawned directly at a spawners position will check the "spawnUsed" flag, for the area spawners we check if their position matches one used in the nav box function
                {
                    int matches = 1;
                    if (spawnPositions.Contains(scrapSpawns[i].transform.position))// these "area" spawners can be used to spawn multiple pieces of scrap, the number of positions in our list is the number of times it called the navbox function
                    {
                        matches = spawnPositions.FindAll(x => x == scrapSpawns[i].transform.position).Count;
                        usedSpawns.AddRange(Enumerable.Repeat(scrapSpawns[i], matches));// add multiple copies to the list depending on how much scrap it spawned
                    }
                    else
                    {
                        usedSpawns.Add(scrapSpawns[i]);
                    }

                    Transform current = scrapSpawns[i].transform;
                    while (current != null)// loop to find Tile parent object of a scrap spawner
                    {
                        if (current.GetComponent<Tile>() != null)
                        {
                            if (tileDict.TryGetValue(current.GetComponent<Tile>(), out int value))
                            {
                                tileDict[current.GetComponent<Tile>()] += matches;// amount of scrap spawned by this spawner on this tile
                            }
                            else
                            {
                                ScienceBirdTweaks.Logger.LogWarning("Tile not found in dictionary!");
                            }
                            break;
                        }
                        else
                        {
                            current = current.parent;
                        }
                    }
                }
            }
            tileDict = tileDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            float totalArea = 0;
            float averageCount = 0;
            foreach (var entry in tileDict)
            {
                float area = (entry.Key.Bounds.size.x * entry.Key.Bounds.size.z) / 100;
                if (float.IsNaN(area) || area == 0f)
                {
                    area = CalculateTileNavigableArea(entry.Key.gameObject) / 100;
                }
                int count = entry.Value;
                ScienceBirdTweaks.Logger.LogDebug($"TILE INFO ({entry.Key.gameObject.name}): {count} scrap, {area} area | (Scrap per area: {count / area})");
                totalArea += area;
                averageCount += count;
            }
            averageCount = averageCount / tileDict.Count;
            ScienceBirdTweaks.Logger.LogInfo($"INTERIOR TILE SUMMARY ({DungeonManager.CurrentExtendedDungeonFlow.DungeonName}, {StartOfRound.Instance.currentLevel.PlanetName}): {tileDict.Count} tiles | Total tile area: {totalArea} | Average tile area: {totalArea / tileDict.Count}");
            ScienceBirdTweaks.Logger.LogInfo($"INTERIOR SCRAP SUMMARY ({DungeonManager.CurrentExtendedDungeonFlow.DungeonName}, {StartOfRound.Instance.currentLevel.PlanetName}): {usedSpawns.Count} scrap | Average scrap per tile: {averageCount} | Scrap per unit area: {usedSpawns.Count / totalArea}");
        }

        public static float CalculateTileNavigableArea(GameObject tileParent)
        {
            MeshRenderer[] renderers = tileParent.GetComponentsInChildren<MeshRenderer>();

            if (renderers.Length == 0)
                return 0f;

            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;

            foreach (MeshRenderer renderer in renderers)
            {
                if (!boundsInitialized)
                {
                    combinedBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
            return combinedBounds.size.x * combinedBounds.size.z;
        }
    }
}
