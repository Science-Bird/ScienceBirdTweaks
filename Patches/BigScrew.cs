using System.Data.Common;
using System.Linq;
using HarmonyLib;
using SelfSortingStorage.Cupboard;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BigScrew
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.PlayerLoadedClientRpc))]
        [HarmonyPostfix]
        static void FindScrewsOnLoad(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            GameObject[] objectList = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "BigBolt(Clone)").ToArray();

            foreach(GameObject obj in objectList)
            {
                obj.GetComponentInChildren<ScanNodeProperties>().headerText = "Big screw";
                obj.GetComponentInChildren<GrabbableObject>().itemProperties.itemName = "Big screw";
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
        [HarmonyPostfix]
        static void FindScrewsShip(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            GameObject[] objectList = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "BigBolt(Clone)").ToArray();

            foreach (GameObject obj in objectList)
            {
                obj.GetComponentInChildren<ScanNodeProperties>().headerText = "Big screw";
                obj.GetComponentInChildren<GrabbableObject>().itemProperties.itemName = "Big screw";
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
        [HarmonyPostfix]
        [HarmonyBefore("zigzag.SelfSortingStorage")]
        static void UpdateScrewName(GrabbableObject __instance)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            if (__instance.itemProperties != null && (__instance.itemProperties.itemName == "Big bolt" || __instance.itemProperties.itemName == "Big screw"))
            {
                __instance.itemProperties.itemName = "Big screw";
                ScanNodeProperties node = __instance.gameObject.GetComponentInChildren<ScanNodeProperties>();
                if (node != null)
                {
                    node.headerText = "Big screw";
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void AddScrewToTerminal(Terminal __instance)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            if (__instance.currentText.ToLower().Contains("big bolt"))
            {
                __instance.currentText = __instance.currentText.Replace("Big bolt", "Big screw");
                __instance.screenText.text = __instance.currentText;
            }
        }
    }
}
