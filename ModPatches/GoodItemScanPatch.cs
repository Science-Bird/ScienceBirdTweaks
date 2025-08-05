using System.Collections.Generic;
using System.Reflection;
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

        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), "UpdateScanNodes"), prefix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("ScanUpdatePatch1")), postfix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("ScanUpdatePatch2")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Scanner), nameof(Scanner.DisableAllScanElements)), prefix: new HarmonyMethod(typeof(GoodItemScanPatches).GetMethod("ScanClearPatch")));
        }

        public static void ScanUpdatePatch1(Scanner __instance, out List<ScanNodeProperties> __state)// scan update prefix
        {
            __state = new List<ScanNodeProperties>();
            if (ScanHighlightPatches.greenHologramMat == null)
            {
                ScanHighlightPatches.MaterialSetup(HUDManager.Instance.hologramMaterial, 0);
            }
            if (ScanHighlightPatches.blueHologramMat == null)
            {
                ScanHighlightPatches.MaterialSetup(HUDManager.Instance.hologramMaterial, 1);
            }

            FieldInfo scannedField = AccessTools.Field(typeof(Scanner), "_scanNodes");
            Dictionary<ScanNodeProperties, int> scanNodeDict = (Dictionary<ScanNodeProperties, int>)scannedField.GetValue(__instance);
            int scanTotal = scanNodeDict.Count;
            if (scanNodeDict == null || scanTotal <= 0) { return; }
            List<GrabbableObject> newScannedObjects = ScanHighlightPatches.ComputeNewScannedObjects(highlights, scanned, scanNodeDict.Keys.ToList());

            __state = scanNodeDict.Keys.ToList();
            scanned = new List<GrabbableObject>(newScannedObjects);
        }

        public static void ScanUpdatePatch2(Scanner __instance, List<ScanNodeProperties> __state)// scan update postfix
        {
            FieldInfo scannedField = AccessTools.Field(typeof(Scanner), "_scanNodes");
            Dictionary<ScanNodeProperties, int> scanNodeDict = (Dictionary<ScanNodeProperties, int>)scannedField.GetValue(__instance);
            int scanTotal = scanNodeDict.Count;
            if (scanTotal != __state.Count)// if scan nodes have changed between prefix and postfix
            {
                List<ScanNodeProperties> newItems = scanNodeDict.Keys.Except(__state).ToList();
                List<ScanNodeProperties> removedItems = __state.Except(scanNodeDict.Keys).ToList();
                if (newItems.Count > 0 || removedItems.Count > 0)
                {
                    List<GrabbableObject> newScannedObjects = ScanHighlightPatches.ComputeNewScannedObjects(highlights, scanned, scanNodeDict.Keys.ToList());
                    scanned = new List<GrabbableObject>(newScannedObjects);
                }
            }
        }

        public static void ScanClearPatch(Scanner __instance)
        {
            //ScienceBirdTweaks.Logger.LogDebug($"Clearing all {highlights.Count} objects");
            foreach (var highlight in highlights)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"Clearing: {highlight.Value.name}");
                Object.Destroy(highlight.Value);
            }
            scanned.Clear();
            highlights.Clear();
        }

        public static void GoodItemScanClearNodes()
        {
            GoodItemScan.GoodItemScan.scanner?.DisableAllScanElements();
        }
    }
}
