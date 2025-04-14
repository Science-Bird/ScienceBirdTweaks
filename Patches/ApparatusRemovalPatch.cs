using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ApparatusRemovalPatch
    {
        private static bool _doBlackout = false;
        public static bool doingHazardShutdown = false;

        [HarmonyPatch(typeof(LungProp), nameof(LungProp.EquipItem))]
        [HarmonyPrefix]
        static void OnApparatusGrab(LungProp __instance)
        {
            if (!__instance.isLungDocked)
                return;

            if (ScienceBirdTweaks.BlackoutOnApparatusRemoval.Value)
                _doBlackout = true;

            if (ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value)
                doingHazardShutdown = true;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FlickerPoweredLights))]
        [HarmonyPostfix]
        static void FlickerPoweredLightsPostfix(RoundManager __instance)
        {
            if (!_doBlackout)
                return;

            if (__instance == null)
            {
                ScienceBirdTweaks.Logger.LogWarning("FlickerPoweredLightsPostfix called but __instance was null.");
                return;
            }

            try
            {
                ScienceBirdTweaks.Logger.LogDebug($"Calling BlackoutOverride for {__instance.gameObject.name}.");
                TrueBlackout.DoBlackout(false);
            }
            catch (System.Exception e)
            {
                ScienceBirdTweaks.Logger.LogError($"Error executing BlackoutOverride after FlickerPoweredLights initiation: {e}");
            }

            _doBlackout = false;
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
                return true;

            HazardShutdown(__instance, switchedOn);

            doingHazardShutdown = false;

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