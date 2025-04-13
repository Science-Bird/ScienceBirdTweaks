using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections;
using DigitalRuby.ThunderAndLightning;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ScienceBirdTweaks.ZapGun
{
    [HarmonyPatch]
    public class ZapPatches
    {
        public static bool goingRight;
        public static bool goingLeft;
        public static GameObject leftArrow;
        public static GameObject rightArrow;
        public static bool doTutorialOverride = false;
        public static int tutorialCount = 2;
        public static Dictionary<int, int> layerDict;
        public static int layersLength;
        public static RectTransform image1;
        public static RectTransform image2;
        public static PatcherTool currentZapInstance;
        public static float tVal = 0f;
        public static float bendInterpolate = 0f;

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.Start))]
        [HarmonyPostfix]
        static void MinePatch(ref Landmine __instance)
        {
            if (ScienceBirdTweaks.ZapGunRework.Value || ScienceBirdTweaks.MineDisableAnimation.Value)
            {
                __instance.gameObject.AddComponent<MineZapper>();
                Scripts.MineAudio audioScript = __instance.gameObject.AddComponent<Scripts.MineAudio>();
                audioScript.audioSource = __instance.gameObject.GetComponent<AudioSource>();
                audioScript.beepClip = HazardPatches.disabledBeep;
            }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.Start))]
        [HarmonyPostfix]
        static void TurretPatch(ref Turret __instance)
        {
            if (ScienceBirdTweaks.ZapGunRework.Value)
            {
                __instance.gameObject.AddComponent<TurretZapper>();
            }
        }

        [HarmonyPatch(typeof(SpikeRoofTrap), nameof(SpikeRoofTrap.Start))]
        [HarmonyPostfix]
        static void SpikesPatch(ref SpikeRoofTrap __instance)
        {
            if (ScienceBirdTweaks.ZapGunRework.Value || ScienceBirdTweaks.SpikeTrapDisableAnimation.Value)
            {
                ScienceBirdTweaks.Logger.LogDebug("Adding spike zapper!");
                GameObject animatorObj = __instance.gameObject.transform.parent.gameObject;
                ScienceBirdTweaks.Logger.LogDebug(animatorObj.name);
                animatorObj.layer = 21;
                Light light = animatorObj.GetComponentInChildren<Light>();
                light.gameObject.layer = 21;
                light.gameObject.AddComponent<SpikesZapper>();
                BoxCollider collider = light.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void SetZapGunProperties(GameNetworkManager __instance)
        {
            Item zapGun = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Zap gun").First();
            if (zapGun != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Found zap gun!");
                zapGun.batteryUsage = ScienceBirdTweaks.ZapGunBattery.Value;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveLocalPlayerValues))]
        [HarmonyPostfix]
        static void FixShockMinigameSave(GameNetworkManager __instance)
        {
            if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Vanilla") { return; }
            try
            {
                if (HUDManager.Instance != null)// no longer requires shock minigame tutorial to be still active in order to save its state, which meant that if the tutorial was finished, this information would not be saved
                {
                    ES3.Save("FinishedShockMinigame", PatcherTool.finishedShockMinigame, "LCGeneralSaveData");
                }
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"ERROR occured while saving local player shockminigame values!: {ex}");
            }
        }


        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SetSavedValues))]
        [HarmonyPostfix]
        static void FixShockMinigameLoad(HUDManager __instance)
        {
            if (ScienceBirdTweaks.ZapGunRework.Value)
            {
                string[] layerPriority = ScienceBirdTweaks.ZapScanPriority.Value.ToLower().Replace(" ","").Split(",");
                layerDict = new Dictionary<int, int>();
                for (int i = 0; i < layerPriority.Length; i++)
                {
                    int num = 0;
                    switch (layerPriority[i])
                    {
                        case "door":
                            num = -1;
                            break;
                        case "enemies":
                            num = 19;
                            break;
                        case "traps":
                            num = 21;
                            break;
                        case "players":
                            num = 3;
                            break;
                        default:
                            continue;
                    }
                    if (num != 0)
                    {
                        layerDict.Add(num, i);
                    }
                }
                layersLength = layerDict.Count;
            }

            if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Vanilla") { return; }

            tutorialCount = ScienceBirdTweaks.ZapGunTutorialCount.Value;
            __instance.setTutorialArrow = false;
            if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Only First Time" && ES3.Load("FinishedShockMinigame", "LCGeneralSaveData", 0) < tutorialCount)
            {
                PatcherTool.finishedShockMinigame = 0;
                __instance.setTutorialArrow = true;
            }
            else if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Every Session" || ScienceBirdTweaks.ZapGunTutorialMode.Value == "Always")
            {
                PatcherTool.finishedShockMinigame = 0;
                __instance.setTutorialArrow = true;
            }
            if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Always")
            {
                doTutorialOverride = true;
            }

            if (ScienceBirdTweaks.ZapGunTutorialRevamp.Value)
            {
                ScienceBirdTweaks.Logger.LogInfo($"Updating controllers!");
                RuntimeAnimatorController arrowController = (RuntimeAnimatorController)ScienceBirdTweaks.TweaksAssets.LoadAsset("ArrowRightAlt");
                leftArrow = __instance.shockTutorialLeftAlpha.gameObject;
                rightArrow = __instance.shockTutorialRightAlpha.gameObject;
                leftArrow.GetComponent<Animator>().runtimeAnimatorController = arrowController;
                rightArrow.GetComponent<Animator>().runtimeAnimatorController = arrowController;
                image1 = leftArrow.transform.Find("Image (1)").gameObject.GetComponent<RectTransform>();
                image2 = rightArrow.transform.Find("Image (1)").gameObject.GetComponent<RectTransform>();
            }
        }


        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.BeginShockingAnomalyOnClient))]
        [HarmonyPostfix]
        static void StoreZapGunInstance(PatcherTool __instance)
        {
            if (__instance.IsOwner && ScienceBirdTweaks.ZapGunTutorialRevamp.Value)
            {
                currentZapInstance = __instance;
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
        [HarmonyPrefix]
        static void TutorialGrabValues(HUDManager __instance, out HUDManager __state )
        {
            __state = __instance;
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
        [HarmonyPostfix]
        static void TutorialAnimChange(HUDManager __instance, HUDManager __state)
        {
            if (!ScienceBirdTweaks.ZapGunTutorialRevamp.Value) { return; }

            if (__instance.tutorialArrowState == 0 || !__instance.setTutorialArrow)
            {
                __instance.shockTutorialLeftAlpha.alpha = Mathf.Lerp(__state.shockTutorialLeftAlpha.alpha, 0f, 17f * Time.deltaTime);
                __instance.shockTutorialRightAlpha.alpha = Mathf.Lerp(__state.shockTutorialRightAlpha.alpha, 0f, 17f * Time.deltaTime);
            }
            else if (__instance.tutorialArrowState == 1)
            {
                float targetInterpolate = Mathf.Clamp((Mathf.Abs(currentZapInstance.bendMultiplier) - 0.3f) / (1f - 0.3f), 0f, 1f);
                if (bendInterpolate > targetInterpolate)
                {
                    bendInterpolate -= Time.deltaTime * 2;
                }
                if (bendInterpolate < targetInterpolate)
                {
                    bendInterpolate += Time.deltaTime * 2;
                }
                if (tVal > bendInterpolate)
                {
                    tVal -= Time.deltaTime;
                }
                if (tVal < bendInterpolate)
                {
                    tVal += Time.deltaTime;
                }
                ScienceBirdTweaks.Logger.LogDebug($"{currentZapInstance.bendMultiplier}, {image1.anchoredPosition.x}, {bendInterpolate}, {Mathf.Clamp(tVal, 0f, 1f)}");
                image1.anchoredPosition = new Vector2(Mathf.Lerp(image1.anchoredPosition.x, 35 - 70 * bendInterpolate, Mathf.Clamp(tVal, 0f, 1f)), 4.2f);
                __instance.shockTutorialLeftAlpha.alpha = Mathf.Lerp(__state.shockTutorialLeftAlpha.alpha, 1f, 17f * Time.deltaTime);
                __instance.shockTutorialRightAlpha.alpha = Mathf.Lerp(__state.shockTutorialRightAlpha.alpha, 0f, 17f * Time.deltaTime);
            }
            else
            {
                float targetInterpolate = Mathf.Clamp((Mathf.Abs(currentZapInstance.bendMultiplier) - 0.3f) / (1f - 0.3f), 0f, 1f);
                if (bendInterpolate > targetInterpolate)
                {
                    bendInterpolate -= Time.deltaTime * 2;
                }
                if (bendInterpolate < targetInterpolate)
                {
                    bendInterpolate += Time.deltaTime * 2;
                }
                if (tVal > bendInterpolate)
                {
                    tVal -= Time.deltaTime;
                }
                if (tVal < bendInterpolate)
                {
                    tVal += Time.deltaTime;
                }
                ScienceBirdTweaks.Logger.LogDebug($"{currentZapInstance.bendMultiplier}, {image2.anchoredPosition.x}, {bendInterpolate}, {Mathf.Clamp(tVal, 0f, 1f)}");
                image2.anchoredPosition = new Vector2(Mathf.Lerp(image1.anchoredPosition.x, 35 - 70 * bendInterpolate, Mathf.Clamp(tVal, 0f, 1f)), 4.2f);
                __instance.shockTutorialRightAlpha.alpha = Mathf.Lerp(__state.shockTutorialRightAlpha.alpha, 1f, 17f * Time.deltaTime);
                __instance.shockTutorialLeftAlpha.alpha = Mathf.Lerp(__state.shockTutorialLeftAlpha.alpha, 0f, 17f * Time.deltaTime);
            }
        }
        


        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.StopShockingAnomalyOnClient))]
        [HarmonyPrefix]
        static void StopShockPrefix(PatcherTool __instance, bool failed, out float __state)
        {
            __state = __instance.timeSpentShocking;// normally time spent shocking is set to zero midway through this method, so we pass it through to our postfix
            if (__instance.IsOwner && !doTutorialOverride)
            { 
                ScienceBirdTweaks.Logger.LogInfo($"Failed: {failed}");
                ScienceBirdTweaks.Logger.LogInfo($"Time spent: {__instance.timeSpentShocking}");
            }
        }

        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.StopShockingAnomalyOnClient))]
        [HarmonyPostfix]
        static void StopShockPostfix(PatcherTool __instance, bool failed, float __state)
        {
            if (__instance.IsOwner && !doTutorialOverride)
            {
                ScienceBirdTweaks.Logger.LogInfo($"Failed: {failed}");
                ScienceBirdTweaks.Logger.LogInfo($"Time spent: {__state}");
                if (__instance.timeSpentShocking == 0f && failed && __state > 0.75f)// equivalent to vanilla check, but vanilla check usually fails due to time spent shocking getting reset to zero before this
                {
                    __instance.SetFinishedShockMinigameTutorial();
                }
            }
        }

        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.SetFinishedShockMinigameTutorial))]
        [HarmonyPrefix]
        static bool TutorialUpdateOverridePrefix(PatcherTool __instance)
        {
            if (ScienceBirdTweaks.ZapGunTutorialMode.Value == "Vanilla")
            {
                return true;// keep vanilla function
            }

            TutorialUpdate(__instance);// replace vanilla function
            return false;
        }

        static void TutorialUpdate(PatcherTool zapGun)
        {
            if (doTutorialOverride)
            {
                PatcherTool.finishedShockMinigame = 0;
                HUDManager.Instance.setTutorialArrow = true;
            }
            else if (HUDManager.Instance.setTutorialArrow)
            {
                PatcherTool.finishedShockMinigame++;
                if (PatcherTool.finishedShockMinigame >= tutorialCount)
                {
                    HUDManager.Instance.setTutorialArrow = false;
                }
            }
        }

        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.ScanGun))]
        [HarmonyPrefix]
        public static bool ZapGunOverridePrefix(PatcherTool __instance, ref IEnumerator __result)
        {
            if (!ScienceBirdTweaks.ZapGunRework.Value)
            {
                return true;// keep vanilla function
            }

            __result = ScanGunPatch(__instance);// entirely replace vanilla scan gun routine
            return false;
        }

        private static int LayerSort(RaycastHit hit)
        {
            int layer = hit.transform.gameObject.layer;
            if (hit.transform.gameObject.name == "BigDoor")// the big doors are also on the map hazard layer since I don't want to include too many layers in my mask (which could accidentally include unintended targets), so I added doors as an edge case
            {
                layer = -1;
            }
            for (int i = 0; i < layersLength; i++)
            {
                if (layerDict.TryGetValue(layer, out int value))
                {
                    return value;
                }
            }
            return layersLength;
        }

        static IEnumerator ScanGunPatch(PatcherTool __instance)
        {
            __instance.anomalyMask = 2621448;// includes enemies, map hazards, and players
            __instance.effectAnimator.SetTrigger("Scan");
            __instance.gunAudio.PlayOneShot(__instance.scanAnomaly);
            __instance.lightningScript = __instance.lightningObject.GetComponent<LightningSplineScript>();
            __instance.lightningDest.SetParent(null);
            __instance.lightningBend1.SetParent(null);
            __instance.lightningBend2.SetParent(null);
            ScienceBirdTweaks.Logger.LogDebug("Scan 1");
            for (int i = 0; i < 12; i++)
            {
                if (__instance.IsOwner)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Scan 2");
                    if (__instance.isPocketed)
                    {
                        yield break;
                    }
                    __instance.ray = new Ray(__instance.playerHeldBy.gameplayCamera.transform.position - __instance.playerHeldBy.gameplayCamera.transform.forward * 3f, __instance.playerHeldBy.gameplayCamera.transform.forward);
                    //Debug.DrawRay(__instance.playerHeldBy.gameplayCamera.transform.position - __instance.playerHeldBy.gameplayCamera.transform.forward * 3f, __instance.playerHeldBy.gameplayCamera.transform.forward * 6f, Color.red, 5f);
                    int num = Physics.SphereCastNonAlloc(__instance.ray, 5f, __instance.raycastEnemies, 5f, __instance.anomalyMask, QueryTriggerInteraction.Collide);
                    ScienceBirdTweaks.Logger.LogDebug($":: {num}, {__instance.raycastEnemies.Length}");

                    RaycastHit[] validHits = new RaycastHit[num];
                    for (int k = 0; k < num; k++)
                    {
                        validHits[k] = __instance.raycastEnemies[k];
                    }

                    // mostly the same as vanilla logic, except for this significantly changed raycast hit sorting
                    validHits = validHits
                        .Where(x => x.collider != null)
                        .OrderBy(LayerSort)
                        .ThenBy(x => x.distance)
                        .ToArray();
                    ScienceBirdTweaks.Logger.LogDebug($":: {num}, {validHits.Length}");
                    foreach (RaycastHit hit in validHits)
                    {
                        if (hit.transform != null)
                        {
                            ScienceBirdTweaks.Logger.LogDebug($"{hit.transform.gameObject.name} ({hit.distance}, {hit.transform.gameObject.layer})");
                        }
                    }
                    for (int j = 0; j < validHits.Length; j++)
                    {
                        __instance.hit = validHits[j];
                        ScienceBirdTweaks.Logger.LogDebug($"TRUE HIT {__instance.hit.transform.gameObject.name} ({__instance.hit.distance}, {__instance.hit.transform.gameObject.layer})");
                        ScienceBirdTweaks.Logger.LogDebug($"HIT BOOLS {!(__instance.hit.transform == null)} ({__instance.hit.transform.gameObject.GetComponent<IShockableWithGun>}, {__instance.hit.transform.gameObject.layer})");
                        if (!(__instance.hit.transform == null) && __instance.hit.transform.gameObject.TryGetComponent<IShockableWithGun>(out var component) && component.CanBeShocked())
                        {
                            Vector3 shockablePosition = component.GetShockablePosition();
                            ScienceBirdTweaks.Logger.LogDebug("Got shockable transform name : " + component.GetShockableTransform().gameObject.name);
                            if (__instance.GunMeetsConditionsToShock(__instance.playerHeldBy, shockablePosition, 60f))
                            {
                                __instance.gunAudio.Stop();
                                __instance.BeginShockingAnomalyOnClient(component);
                                yield break;
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(0.125f);
            }
            ScienceBirdTweaks.Logger.LogDebug("Zap gun light off!!!");
            __instance.SwitchFlashlight(on: false);
            __instance.isScanning = false;
        }
    }
}
