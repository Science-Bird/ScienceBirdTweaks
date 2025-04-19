using System;
using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BlackoutTriggerPatches
    {
        private static bool _doBlackout = false;
        public static bool doingHazardShutdown = false;
        public static bool doingHazardStartup = false;
        private static bool _inSwitchContext = false;
        private static float hazardWait = -1f;
        private static bool breakerDone = false;

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

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOnClientRpc))]// includes breaker switch flipped on
        [HarmonyPrefix]
        static void LightsOnStart()
        {
            ScienceBirdTweaks.Logger.LogDebug("Breaker start ON!");
            if (ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value)
            {
                hazardWait = 0.49f;
                doingHazardStartup = true;
            }
        }


        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]// includes breaker switch flipped off
        [HarmonyPrefix]
        static void LightsOffStart()
        {
            ScienceBirdTweaks.Logger.LogDebug("Breaker start! OFF");
            if (ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value && !doingHazardShutdown)
            {
                hazardWait = 0.49f;
                doingHazardShutdown = true;
            }
        }

        [HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void BreakerFinished(RoundManager __instance)// often with custom animations enabled, the process is still going on at this point, so the actual end of the process is handled by a timer (with this as a supporting value)
        {
            if (ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value && (doingHazardShutdown || doingHazardStartup))
            {
                breakerDone = true;
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.Update))]
        [HarmonyPostfix]
        static void ShutdownTimer()// will end shutdown/startup after slightly less than 0.5 seconds (breaker switch interact cooldown is 0.5 seconds)
        {
            if (ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value && (doingHazardShutdown || doingHazardStartup))
            {
                if (hazardWait < 0f && breakerDone)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Stopping hazard routine after wait!");
                    if (doingHazardShutdown)
                        doingHazardShutdown = false;

                    if (doingHazardStartup)
                        doingHazardStartup = false;

                    breakerDone = false;
                }
                else if (hazardWait > 0f)
                {
                    hazardWait -= Time.deltaTime;
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ReadDialogue))]
        [HarmonyPrefix]
        public static void DialoguePatch(HUDManager __instance, ref DialogueSegment[] dialogueArray)// apparatus has convenient long delay function that runs after (and cant be immediately reversed), so no goofy timer needed
        {
            ScienceBirdTweaks.Logger.LogDebug("Apparatus shutdown finished!");
            doingHazardShutdown = false;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPostfix]
        static void ResetHazardFlag()// reset at end of round just in case
        {
            doingHazardShutdown = false;
            doingHazardStartup = false;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPostfix]
        static void ResetOnLanding()// this should handle if a shutdown gets initiated at start of game because of power outage event (idk havent tested)
        {
            doingHazardShutdown = false;
            doingHazardStartup = false;
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
            if ((ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value || ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value) && !__instance.isBigDoor)
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
            if ((!ScienceBirdTweaks.DisableTrapsOnApparatusRemoval.Value && !ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value) || __instance.isBigDoor)
                return true;

            HazardShutdown(__instance, switchedOn);

            return false;
        }

        public static void HazardShutdown(TerminalAccessibleObject terminalObj, bool switchedOn)
        {
            if (!switchedOn && doingHazardShutdown && !terminalObj.isBigDoor)
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
            else if (switchedOn && ScienceBirdTweaks.DisableTrapsOnBreakerSwitch.Value && doingHazardStartup && !terminalObj.isBigDoor && terminalObj.inCooldown)
            {
                Landmine mine = terminalObj.gameObject.GetComponent<Landmine>();
                Turret turret = terminalObj.gameObject.GetComponent<Turret>();
                SpikeRoofTrap spikes = terminalObj.gameObject.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>();
                terminalObj.mapRadarText.color = Color.green;
                terminalObj.mapRadarBox.color = Color.green;
                if (mine != null)
                {
                    mine.ToggleMine(true);
                }
                else if (turret != null)
                {
                    turret.ToggleTurretEnabled(true);
                }
                else if (spikes != null)
                {
                    spikes.ToggleSpikesEnabled(true);
                }
                terminalObj.inCooldown = false;
            }
        }
    }
}