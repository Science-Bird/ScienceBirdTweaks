using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ApparatusRemovalPatch
    {
        public static bool doingHazardShutdown = false;

        [HarmonyPatch(typeof(LungProp), nameof(LungProp.DisconnectFromMachinery))]
        [HarmonyPrefix]
        static void OnDisconnectTest1(LungProp __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Appy prefix!");
            if (ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value)
            {
                doingHazardShutdown = true;
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.TurnOnAllLights))]
        [HarmonyPostfix]
        static void OnDisconnectTest2(RoundManager __instance, bool on)
        {
            if (ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value && !on)
            {
                ScienceBirdTweaks.Logger.LogDebug("Turning off lights!");
                doingHazardShutdown = true;
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.RadiationWarningHUD))]
        [HarmonyPostfix]
        static void OnDisconnectFromMachinery(HUDManager __instance)
        {
            if (!ScienceBirdTweaks.BlackoutOnApparatusRemoval.Value)
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
                TrueBlackout.DoBlackout(false);
            }
            catch (System.Exception e)
            {
                ScienceBirdTweaks.Logger.LogError($"Error executing BlackoutOverride after DisconnectFromMachinery initiation: {e}");
            }
        }


        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.OnPowerSwitch))]
        [HarmonyPostfix]
        public static void HazardShutdown(TerminalAccessibleObject __instance)// this doesn't work yet lol
        {
            if (doingHazardShutdown && ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value && !__instance.isBigDoor)
            {
                ScienceBirdTweaks.Logger.LogDebug("Hazard shutdown!");
                Landmine mine = __instance.gameObject.GetComponent<Landmine>();
                Turret turret = __instance.gameObject.GetComponent<Turret>();
                SpikeRoofTrap spikes = __instance.gameObject.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>();
                __instance.mapRadarText.color = Color.gray;
                __instance.mapRadarBox.color = Color.gray;
                if (mine != null)
                {
                    mine.ToggleMine(false);
                }
                else if (turret != null)
                {
                    turret.ToggleTurretEnabled(false);
                }
                else if (spikes != null)
                {
                    spikes.ToggleSpikesEnabled(false);
                }
                __instance.inCooldown = true;
            }
        }
    }
}