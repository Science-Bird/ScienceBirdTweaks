using System.Reflection;
using System;
using HarmonyLib;
using System.Runtime.CompilerServices;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ElevatorJinglePatch
    {
        internal static System.Random clipRandom;

        internal static bool butteryPresent = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void CheckButtery(StartOfRound __instance)// check if HalloweenElevator present, then do not run patches
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "HalloweenElevator")
                {
                    butteryPresent = true;
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(MineshaftElevatorController), nameof(MineshaftElevatorController.Update))]
        [HarmonyPrefix]
        private static void SetJingleClip(MineshaftElevatorController __instance)
        {
            if (butteryPresent || !ScienceBirdTweaks.OldHalloweenElevatorMusic.Value || RoundManager.Instance.currentMineshaftElevator != __instance || __instance.elevatorHalloweenClips.Length != __instance.elevatorHalloweenClipsLoop.Length || __instance.elevatorHalloweenClips.Length == 0 || __instance.elevatorHalloweenClipsLoop.Length == 0)
            {
                return;
            }

            if (__instance.playMusic && !__instance.elevatorJingleMusic.isPlaying)// halloween clips still exist and are even loaded onto elevator object, they just normally aren't used
            {
                if (clipRandom == null)
                {
                    clipRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
                }
                int clipIndex = clipRandom.Next(0, __instance.elevatorHalloweenClips.Length);
                if (__instance.elevatorMovingDown)
                {
                    __instance.elevatorJingleMusic.clip = __instance.elevatorHalloweenClips[clipIndex];
                }
                else
                {
                    __instance.elevatorJingleMusic.clip = __instance.elevatorHalloweenClipsLoop[clipIndex];
                }
            }
        }

        [HarmonyPatch(typeof(MineshaftElevatorController), nameof(MineshaftElevatorController.OnEnable))]
        [HarmonyPrefix]
        private static void SetJingleRandom(MineshaftElevatorController __instance)
        {
            if (butteryPresent || !ScienceBirdTweaks.OldHalloweenElevatorMusic.Value)
            {
                return;
            }
            clipRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        }
    }
}
