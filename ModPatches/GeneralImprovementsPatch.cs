using System;
using System.Reflection;
using HarmonyLib;
using GeneralImprovements;

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
        private static FieldInfo lightningTarget;

        public static void StartSetup()
        {
            hudPatches = AccessTools.TypeByName("GeneralImprovements.Patches.HUDManagerPatch");
            if (hudPatches != null)
            {
                lightningTarget = AccessTools.Field(hudPatches, "CurrentLightningTarget");

            }
        }

        public static void UpdatePatch(StormyWeather __instance)
        {
            if (lightningTarget != null && __instance.setStaticToObject == null)
            {
                lightningTarget.SetValue(hudPatches, null);
            }
        }
    }
}
