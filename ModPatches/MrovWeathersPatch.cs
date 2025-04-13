using System.Reflection;
using HarmonyLib;
using ScienceBirdTweaks.Scripts;

namespace ScienceBirdTweaks.ModPatches
{
    public class MrovWeathersPatch
    {
        public static void DoPatching()
        {
            System.Type blackoutType = AccessTools.TypeByName("MrovWeathers.Blackout");
            if (blackoutType != null)
            {
                MethodInfo onEnable = AccessTools.Method(blackoutType, "OnEnable");
                MethodInfo customMethod = typeof(TrueBlackoutPatch).GetMethod(nameof(TrueBlackoutPatch.BlackoutOverridePrefix), BindingFlags.Static | BindingFlags.Public);
                if (onEnable != null && customMethod != null && ScienceBirdTweaks.Harmony != null)
                {
                    ScienceBirdTweaks.Harmony.Patch(onEnable, prefix: new HarmonyMethod(customMethod));
                }
            }
        }
    }

    public static class TrueBlackoutPatch
    {
        public static bool BlackoutOverridePrefix(object __instance) // replace existing blackout method
        {
            TrueBlackout.DoBlackout(true);
            return false;
        }
    }
}
