using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;

// Temp trigger for testing floodlight speen

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static bool spinnerAdded = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void AddSpinnerComponentPatch(StartOfRound __instance)
        {
            if (!spinnerAdded && __instance.gameObject.GetComponent<ShipLightsSpinner>() == null)
            {
                ScienceBirdTweaks.Logger.LogInfo("StartOfRound started, attempting to add ShipLightsSpinner.");
                __instance.gameObject.AddComponent<ShipLightsSpinner>();
                spinnerAdded = true;
                ScienceBirdTweaks.Logger.LogInfo("ShipLightsSpinner component added.");
            }
            else if (spinnerAdded)
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipLightsSpinner already added, skipping.");
            }
            else if (__instance.gameObject.GetComponent<ShipLightsSpinner>() != null)
            {
                ScienceBirdTweaks.Logger.LogWarning("ShipLightsSpinner component already exists on StartOfRound object but wasn't tracked. Marking as added.");
                spinnerAdded = true;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix]
        static void ResetSpinnerFlagPatch()
        {
            ScienceBirdTweaks.Logger.LogInfo("Disconnect detected, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "OnDestroy")]
        [HarmonyPostfix]
        static void OnDestroyResetSpinnerFlagPatch()
        {
            ScienceBirdTweaks.Logger.LogInfo("StartOfRound OnDestroy, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }
    }
}