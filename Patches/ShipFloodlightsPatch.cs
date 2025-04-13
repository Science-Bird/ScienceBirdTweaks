using HarmonyLib;
using ScienceBirdTweaks.Scripts;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class ShipFloodlightsPatch
    {
        private static bool spinnerAdded = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void AddSpinnerComponentPatch(StartOfRound __instance)
        {
            if (!spinnerAdded && __instance.gameObject.GetComponent<ShipFloodlightController>() == null)
            {
                __instance.gameObject.AddComponent<ShipFloodlightController>();
                spinnerAdded = true;
                ScienceBirdTweaks.Logger.LogInfo("ShipFloodlightController component added.");
            }
            else if (spinnerAdded)
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController already added, skipping.");
            }
            else if (__instance.gameObject.GetComponent<ShipFloodlightController>() != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController component already exists on StartOfRound object but wasn't tracked. Marking as added.");
                spinnerAdded = true;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix]
        static void ResetSpinnerFlagPatch()
        {
            ScienceBirdTweaks.Logger.LogDebug("Disconnect detected, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "OnDestroy")]
        [HarmonyPostfix]
        static void OnDestroyResetSpinnerFlagPatch()
        {
            ScienceBirdTweaks.Logger.LogDebug("StartOfRound OnDestroy, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "ReviveDeadPlayers")]
        [HarmonyPostfix]
        static void ResetLightsOnLoadPatch(StartOfRound __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Ship reached orbit, attempting to reset floodlights.");

            if (__instance == null)
            {
                ScienceBirdTweaks.Logger.LogError("ResetLightsOnLoadPatch: __instance is null!");
                return;
            }

            ShipFloodlightController controller = __instance.GetComponent<ShipFloodlightController>();

            if (controller != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Found ShipFloodlightController. Calling ResetFloodlightLights().");
                controller.ResetFloodlightLights();
            }
            else
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController component not found on StartOfRound object during ResetLightsOnLoadPatch. Cannot reset lights.");
            }
        }
    }
}