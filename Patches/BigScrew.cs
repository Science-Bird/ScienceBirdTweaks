using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BigScrew
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
        [HarmonyPostfix]
        static void FindScrews(RoundManager __instance)
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
