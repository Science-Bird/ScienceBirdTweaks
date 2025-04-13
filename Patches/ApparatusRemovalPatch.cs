using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ApparatusRemovalPatch
    {
        public static bool doingHazardShutdown = false;

        [HarmonyPatch(typeof(LungProp), nameof(LungProp.EquipItem))]
        [HarmonyPrefix]
        static void OnDisconnectTest1(LungProp __instance)
        {
            if (ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value && __instance.isLungDocked)
            {
                doingHazardShutdown = true;
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.RadiationWarningHUD))]
        [HarmonyPostfix]
        static void OnDisconnectFromMachinery(HUDManager __instance)
        {
            if (!ScienceBirdTweaks.BlackoutOnApparatusRemoval.Value)
                return;

            doingHazardShutdown = false;

            if (__instance == null)
            {
                ScienceBirdTweaks.Logger.LogWarning("LungProp_Disconnect_Patch called but __instance was null.");
                return;
            }

            ScienceBirdTweaks.Logger.LogDebug($"Apparatus removal coroutine (DisconnectFromMachinery) initiated for {__instance.gameObject.name}. Triggering blackout via Postfix.");

            try
            {
                ScienceBirdTweaks.Logger.LogDebug($"Calling TrueBlackoutPatch.BlackoutOverridePrefix for {__instance.gameObject.name}.");
                TrueBlackout.DoBlackout(false);
            }
            catch (System.Exception e)
            {
                ScienceBirdTweaks.Logger.LogError($"Error executing BlackoutOverride after DisconnectFromMachinery initiation: {e}");
            }
        }

        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.Start))]
        [HarmonyPostfix]
        public static void SetPowerSwitchable(TerminalAccessibleObject __instance)
        {
            if (!__instance.isBigDoor)
            {
                PowerSwitchable powerSwitch = __instance.gameObject.AddComponent<PowerSwitchable>();
                OnSwitchPowerEvent switchEvent = new OnSwitchPowerEvent();
                switchEvent.AddListener(__instance.OnPowerSwitch);
                powerSwitch.powerSwitchEvent = switchEvent;
            }
        }


        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.OnPowerSwitch))]
        [HarmonyPrefix]
        public static bool PowerSwitchPrefix(TerminalAccessibleObject __instance, bool switchedOn)
        {
            if (__instance.isBigDoor)
            {
                return true;
            }
            HazardShutdown(__instance, switchedOn);
            return false;
        }

        public static void HazardShutdown(TerminalAccessibleObject terminalObj, bool switchedOn)
        {
            if (!switchedOn && doingHazardShutdown && ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value && !terminalObj.isBigDoor)
            {
                Landmine mine = terminalObj.gameObject.GetComponent<Landmine>();
                Turret turret = terminalObj.gameObject.GetComponent<Turret>();
                SpikeRoofTrap spikes = terminalObj.gameObject.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>();
                terminalObj.mapRadarText.color = Color.gray;
                terminalObj.mapRadarBox.color = Color.gray;
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
                terminalObj.inCooldown = true;
            }
        }
    }
}