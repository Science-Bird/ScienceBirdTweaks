using UnityEngine;
using HarmonyLib;
using System;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class FallRotationPatch
    {
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.FallWithCurve))]
        [HarmonyPostfix]
        static void AfterFall(GrabbableObject __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (__instance.itemProperties == null)
            {
                return;
            }
            try
            {
                if (__instance.itemProperties.name == "WitheredRobotToy" || __instance.itemProperties.name == "WitheredPhone" || __instance.itemProperties.name == "WitheredDentures" || !ScienceBirdTweaks.FallingRotationFix.Value)
                {
                    return;
                }
                if (__instance.startFallingPosition.y - __instance.targetFloorPosition.y > 5f)
                {
                    float curveVal = Mathf.Clamp(StartOfRound.Instance.objectFallToGroundCurveNoBounce.Evaluate(__instance.fallTime), 0f, 1f);
                    if (curveVal < 1f)
                    {
                        if (__instance.floorYRot == -1)
                        {
                            __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.Euler(__instance.itemProperties.restingRotation.x, __instance.transform.eulerAngles.y, __instance.itemProperties.restingRotation.z), curveVal * 0.3f);
                        }
                        else
                        {
                            __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.Euler(__instance.itemProperties.restingRotation.x, __instance.floorYRot + __instance.itemProperties.floorYOffset + 90f, __instance.itemProperties.restingRotation.z), curveVal * 0.3f);
                        }
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Fall complete!");
                        __instance.fallTime = 1.02f;
                    }
                }
            }
            catch (Exception e)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception on fall rotation: {e}");
            }
            //ScienceBirdTweaks.Logger.LogInfo($"FALL DUMP: name: {__instance.itemProperties.name} num: {__instance.startFallingPosition.y - __instance.targetFloorPosition.y}, rotation: {__instance.transform.eulerAngles.y}, localPosition: {__instance.transform.localPosition}, restingRotation: {__instance.itemProperties.restingRotation.y}, yOffset: {__instance.itemProperties.floorYOffset}, yRot: {__instance.floorYRot}, fallTime: {__instance.fallTime}, deltaTime: {Time.deltaTime}");
        }
    }
}

