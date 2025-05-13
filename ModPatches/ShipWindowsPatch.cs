using HarmonyLib;
using System.Reflection;

namespace ScienceBirdTweaks.ModPatches
{
    public class ShipWindowsPatch
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ShipHasLeft"), postfix: new HarmonyMethod(typeof(CloseShutterPatch).GetMethod("OnEndOfRound")));
            //ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(ShipWindows.Patches.Shutters.HideMoonTransitionPatch), nameof(ShipWindows.Patches.Shutters.HideMoonTransitionPatch.HideMoonTransition)), postfix: new HarmonyMethod(typeof(CloseShutterPatch).GetMethod("WindowMetaPatch")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), "ReviveDeadPlayers"), postfix: new HarmonyMethod(typeof(CloseShutterPatch).GetMethod("OnOrbit")));
        }
    }

    public class CloseShutterPatch
    {
        public static bool closed = false;
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
    }
}
