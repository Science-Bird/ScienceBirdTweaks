using HarmonyLib;
using ScienceBirdTweaks.Scripts;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ApparatusRemovalPatch
    {
        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.RadiationWarningHUD))]
        [HarmonyPostfix]
        static void OnDisconnectFromMachinery(HUDManager __instance)
        {
            ScienceBirdTweaks.Logger.LogWarning("Calling LungProp_Disconnect_Patch");

            if (ScienceBirdTweaks.BlackoutOnApparatusRemoval.Value == false)
                return;

            if (__instance == null)
            {
                ScienceBirdTweaks.Logger.LogWarning("LungProp_Disconnect_Patch called but __instance was null.");
                return;
            }

            ScienceBirdTweaks.Logger.LogInfo($"Apparatus removal coroutine (DisconnectFromMachinery) initiated for {__instance.gameObject.name}. Triggering blackout via Postfix.");

            try
            {
                ScienceBirdTweaks.Logger.LogInfo($"Calling TrueBlackoutPatch.BlackoutOverridePrefix for {__instance.gameObject.name}.");
                FullDark.DoFullDark(false);
            }
            catch (System.Exception e)
            {
                ScienceBirdTweaks.Logger.LogError($"Error executing BlackoutOverride after DisconnectFromMachinery initiation: {e}");
            }
        }
    }
}