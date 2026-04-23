using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.ModPatches
{
    public class GeneralImprovementsPatch
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.Start)), postfix: new HarmonyMethod(typeof(LightningFix).GetMethod("StartSetup")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StormyWeather), nameof(StormyWeather.Update)), postfix: new HarmonyMethod(typeof(LightningFix).GetMethod("UpdatePatch")));
        }
    }

    public static class LightningFix
    {
        private static Type hudPatches;
        private static FieldInfo lightningSlots;

        public static void StartSetup()
        {
            hudPatches = AccessTools.TypeByName("GeneralImprovements.Patches.HUDManagerPatch");
            if (hudPatches != null)
            {
                lightningSlots = AccessTools.Field(hudPatches, "_lightningSlotsToOverlays");

            }
        }

        public static void UpdatePatch(StormyWeather __instance)
        {
            if (lightningSlots != null && __instance.targetingMetalObject != null && __instance.setStaticToObject == null)
            {
                Dictionary<int, SpriteRenderer> lightningDict = (Dictionary<int, SpriteRenderer>)lightningSlots.GetValue(hudPatches);
                if (lightningDict != null)
                {
                    foreach (SpriteRenderer sprite in lightningDict.Values)
                    {
                        sprite.enabled = false;
                    }
                }
            }
        }
    }
}
