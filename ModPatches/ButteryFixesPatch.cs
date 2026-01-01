using HarmonyLib;
using UnityEngine;
using ScienceBirdTweaks.Patches;

namespace ScienceBirdTweaks.ModPatches
{
    public class ButteryFixesPatch
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget)), prefix: new HarmonyMethod(typeof(ButteryPatches).GetMethod("RadarLinePatch"), before: ["butterystancakes.lethalcompany.butteryfixes"]));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(ButteryFixes.Utility.NonPatchFunctions), "GetTrueExitPoint"), prefix: new HarmonyMethod(typeof(ButteryPatches).GetMethod("ExitPointsPrefix")));
        }
    }

    public class ButteryPatches
    {
        public static Vector3 currentPos = Vector3.zero;

        public static void RadarLinePatch(ManualCameraRenderer __instance)
        {
            if (__instance.cam == __instance.mapCamera)
            {
                currentPos = __instance.mapCamera.transform.position - Vector3.up * 3.75f;
            }
        }

        public static bool ExitPointsPrefix(ref Vector3 __result)
        {
            __result = NewExitPoints();
            return false;
        }

        public static Vector3 NewExitPoints()
        {
            float posDiff = 1000000f;
            int index = -1;
            for (int i = 0; i < MonitorPatches.entrancePositions.Count; i++)
            {
                float diff = (currentPos - MonitorPatches.entrancePositions[i]).sqrMagnitude;
                if (diff < posDiff)
                {
                    posDiff = diff;
                    index = i;
                }
            }
            if (index >= 0)
            {
                return MonitorPatches.entrancePositions[index];
            }
            else if (MonitorPatches.entrancePositions.Count > 0)
            {
                return MonitorPatches.entrancePositions[0];
            }
            return currentPos;

        }

    }
}
