using HarmonyLib;
using System.Reflection;
using WeatherRegistry;
using System.Linq;
using ShipWindows.ShutterSwitch;
using UnityEngine;

namespace ScienceBirdTweaks.ModPatches
{
    public class ShipWindowsPatch
    {
        public static void DoPatching()
        {
            if (ScienceBirdTweaks.ShipWindowsShutterFix.Value)
            {
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ShipHasLeft"), postfix: new HarmonyMethod(typeof(ShutterPatches).GetMethod("OnEndOfRound")));
                //ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(ShipWindows.Patches.Shutters.HideMoonTransitionPatch), nameof(ShipWindows.Patches.Shutters.HideMoonTransitionPatch.HideMoonTransition)), postfix: new HarmonyMethod(typeof(CloseShutterPatch).GetMethod("WindowMetaPatch")));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ReviveDeadPlayers"), postfix: new HarmonyMethod(typeof(ShutterPatches).GetMethod("OnOrbit")));
            }

            if (ScienceBirdTweaks.ShipWindowsShutterAudio.Value != "Unchanged")
            {
                ShutterPatches.playSFX = ScienceBirdTweaks.ShipWindowsShutterAudio.Value != "Muted";
                ShutterPatches.enableAudio = true;
                if (ScienceBirdTweaks.ShipWindowsShutterAudio.Value == "Only Open")
                {
                    ShutterPatches.enableAudio = false;
                }
                MethodInfo method = typeof(ShipWindows.ShutterSwitch.ShutterSwitchBehavior).GetMethods().FirstOrDefault(x => x.Name == "ToggleSwitch" && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(bool));
                ScienceBirdTweaks.Harmony?.Patch(method, prefix: new HarmonyMethod(typeof(ShutterPatches).GetMethod("OnToggle")));
            }
        }
    }

    public class ShutterPatches
    {
        public static bool closed = false;
        public static bool playSFX = true;
        public static bool enableAudio = true;

        public static void OnEndOfRound(StartOfRound __instance)
        {
            if (!closed)
            {
                ShipWindows.Patches.Shutters.HideMoonTransitionPatch.HideMoonTransition();// this patch normally only runs during routing transitions, so we call the patch manually at takeoff ourselves
                closed = true;
            }
        }

        public static void OnOrbit(StartOfRound __instance)
        {
            closed = false;
        }

        public static bool OnToggle(ShutterSwitchBehavior __instance, bool enable, bool locked)
        {
            __instance.animator.SetBool(ShutterSwitchBehavior.EnabledAnimatorHash, enable);
            __instance.interactTrigger.interactable = !locked;
            if (playSFX)
            {
                __instance.shutterSound.PlayOneShot(enableAudio ? __instance.enableSound : __instance.disableSound);
            }
            return false;
        }
    }
}
