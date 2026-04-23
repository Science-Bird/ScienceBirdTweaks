using System.Collections.Generic;
using GoodItemScan;
using HarmonyLib;
using ScienceBirdTweaks.Patches;
using UnityEngine;
using System.Linq;

namespace ScienceBirdTweaks.ModPatches
{
    public class GoodItemScanPatches
    {
        public static List<GrabbableObject> scanned = new List<GrabbableObject>();
        public static Dictionary<GrabbableObject, GameObject> highlights = new Dictionary<GrabbableObject, GameObject>();
        public static List<ScanNodeProperties> lastScan = new List<ScanNodeProperties>();
        private static bool staleData = true;

        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), "UpdateScanNodes"), postfix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("ScanUpdatePatch")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), nameof(Scanner.DisableAllScanElements)), prefix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("ScanClearPatch")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), "DisableScanNode"), prefix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("OnNodeDisable")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), nameof(Scanner.AssignNodeToUIElement)), prefix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("OnNodeAdd")));
        }

        public static void ScanUpdatePatch(Scanner __instance)// scan update postfix
        {
            if (!staleData) { return; }
            HashSet<ScannedNode> scanNodes = __instance.activeNodes;
            if (scanNodes != null && scanNodes.Count > 0)
            {
                staleData = false;
                List<ScanNodeProperties> scanNodeProperties = scanNodes.Select(x => x.ScanNodeProperties).ToList();
                List<ScanNodeProperties> newItems = scanNodeProperties.Except(lastScan).ToList();
                List<ScanNodeProperties> removedItems = lastScan.Except(scanNodeProperties).ToList();
                if (newItems.Count > 0 || removedItems.Count > 0)
                {
                    List<GrabbableObject> newScannedObjects = ScanHighlightPatches.ComputeNewScannedObjects(highlights, scanned, scanNodeProperties);
                    scanned = new List<GrabbableObject>(newScannedObjects);
                }
                lastScan = scanNodeProperties;
            }
            else
            {
                ScanClear();
            }
        }

        public static void ScanClearPatch(Scanner __instance)
        {
            ScanClear();
        }

        public static void OnNodeDisable(Scanner __instance, ScannedNode scannedNode)
        {
            if (scannedNode.rectTransform != null)
            {
                staleData = true;
            }
        }

        public static void OnNodeAdd(Scanner __instance)
        {
            if (HUDManager.Instance != null)
            {
                staleData = true;
            }
        }

        public static void ScanClear()
        {
            staleData = false;
            //ScienceBirdTweaks.Logger.LogDebug($"Clearing all {highlights.Count} objects");
            foreach (var highlight in highlights)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"Clearing: {highlight.Value.name}");
                Object.Destroy(highlight.Value);
            }
            scanned.Clear();
            highlights.Clear();
            lastScan = new List<ScanNodeProperties>();
        }

        public static void GoodItemScanClearNodes()
        {
            GoodItemScan.GoodItemScan.scanner?.DisableAllScanElements();
        }
    }
}
