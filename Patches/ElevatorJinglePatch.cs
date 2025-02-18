using System.Reflection;
using System;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch(typeof(MineshaftElevatorController))]
    public class ElevatorJinglePatch
    {
        internal static System.Random clipRandom;

        internal static bool butteryPresent = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void CheckButtery(StartOfRound __instance)
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

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void SetJingleClip(MineshaftElevatorController __instance)
        {
            if (butteryPresent || !ScienceBirdTweaks.OldHalloweenElevatorMusic.Value)
            {
                return;
            }
            if (__instance.playMusic && !__instance.elevatorJingleMusic.isPlaying)
            {
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

        [HarmonyPatch("OnEnable")]
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
