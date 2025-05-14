using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BigScrew
    {
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void FindScrewsOnLoad(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            GrabbableObject[] bigBolts = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(obj => obj.itemProperties != null && obj.itemProperties.itemName == "Big bolt").ToArray();

            foreach (GrabbableObject bolt in bigBolts)
            {
                if (bolt.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
                {
                    bolt.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = "Big screw";
                }
                bolt.itemProperties.itemName = "Big screw";
            }
        }


        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void FindScrewsOnLoad(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.BigScrew.Value)
            {
                return;
            }
            if (sceneName == "SampleSceneRelay")
            {
                GrabbableObject[] bigBolts = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(obj => obj.itemProperties != null && obj.itemProperties.itemName == "Big bolt").ToArray();

                foreach (GrabbableObject bolt in bigBolts)
                {
                    if (bolt.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
                    {
                        bolt.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = "Big screw";
                    }
                    bolt.itemProperties.itemName = "Big screw";
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void AddScrewToTerminal(Terminal __instance)// probably not needed, but just in case
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
