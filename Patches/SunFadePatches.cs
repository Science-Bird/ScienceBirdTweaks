using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class SunFadePatches
    {
        private static MeshRenderer sunTexture;
        private static Material originalSunMat;
        private static Material fadingSunMat;
        private static Color baseSunColour;
        private static Color baseEmissiveColour;
        private static bool hasEmissive = false;
        private static bool doFade = false;
        private static int lastTime = -3;

        [HarmonyPatch(typeof(animatedSun), nameof(animatedSun.Start))]
        [HarmonyPostfix]
        static void OnStart()
        {
            if (ScienceBirdTweaks.SunFade.Value && TimeOfDay.Instance != null)
            {
                Transform sunTexObj = TimeOfDay.Instance.sunAnimator.gameObject.transform.Find("SunTexture");
                if (sunTexObj != null)
                {
                    sunTexture = sunTexObj.GetComponent<MeshRenderer>();
                    if (sunTexture != null)
                    {
                        originalSunMat = sunTexture.sharedMaterials[0];
                        fadingSunMat = new Material(originalSunMat);
                        baseSunColour = fadingSunMat.color;
                        if (fadingSunMat.HasColor("_EmissiveColor"))
                        {
                            baseEmissiveColour = fadingSunMat.GetColor("_EmissiveColor");
                            hasEmissive = true;
                        }
                        doFade = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.MoveTimeOfDay))]
        [HarmonyPostfix]
        static void TimeUpdate(TimeOfDay __instance)// this doesn't look great, but I polished it enough to be generally functional. hard to make it look good when the sun doesn't have actual transparency
        {
            if (doFade && __instance.sunAnimator != null)
            {
                // sundown at 63, night at 90
                int integerTime = Mathf.FloorToInt(__instance.normalizedTimeOfDay * 100f) - 63;
                if (integerTime >= lastTime + 3 && lastTime <= 24)// discrete updates throughtout sundown -> night transition
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Fading sun at {integerTime} ({lastTime + 3})!");
                    float multiplier = Mathf.Clamp(1 - 0.125f * (((lastTime + 3) / 3) + 1), 0f, 1f);// basically: keep subtracting from 1 based on how far between sundown and night we are, until eventually we reach 0
                    float lightMultiplier = Mathf.Clamp(0.55f * multiplier, 0f, 0.55f);
                    // sun texture uses alpha clipping so multiplying the alpha just makes it shrink in size a bit, it vanishes at <50% opacity
                    fadingSunMat.color = baseSunColour.RGBMultiplied(multiplier).AlphaMultiplied(lightMultiplier + 0.45f);

                    if (hasEmissive)
                    {
                        fadingSunMat.SetColor("_EmissiveColor", baseEmissiveColour.RGBMultiplied(multiplier));
                    }
                    lastTime += 3;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
        [HarmonyPostfix]
        static void SunLateUpdate(StartOfRound __instance)// replace sun texture after animator
        {
            if (doFade && TimeOfDay.Instance.sunAnimator != null && sunTexture != null)
            {
                sunTexture.material = fadingSunMat;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPostfix]
        static void ResetValues()
        {
            if (ScienceBirdTweaks.SunFade.Value)
            {
                sunTexture = null;
                originalSunMat = null;
                fadingSunMat = null;
                hasEmissive = false;
                doFade = false;
                lastTime = -3;
            }
        }
    }
}
