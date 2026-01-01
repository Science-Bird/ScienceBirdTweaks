using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class StormyPatches
    {

        [HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.Update))]
        [HarmonyPrefix]
        static void OnUpdate(StormyWeather __instance)
        {
            if (ScienceBirdTweaks.LingeringLightningFix.Value && __instance.setStaticToObject != null && __instance.setStaticToObject.GetComponent<GrabbableObject>() && __instance.setStaticToObject.GetComponent<GrabbableObject>().isInFactory)
            {
                //ScienceBirdTweaks.Logger.LogDebug("STOPPING LIGHTNING");
                __instance.staticElectricityParticle.Stop();
                __instance.staticElectricityParticle.gameObject.GetComponent<AudioSource>().Stop();
                __instance.strikeMetalObjectTimer = 0f;
                __instance.setStaticToObject = null;
                __instance.targetingMetalObject = null;
            }
        }
    }
}

