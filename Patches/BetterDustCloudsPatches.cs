using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BetterDustClouds
    {
        private static bool initialSet = true;

        private static bool enableBuffer = true;

        private static bool transitionBuffer = false;

        private static bool transitionOverride = true;

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        [HarmonyPostfix]
        static void ReEnable(TimeOfDay __instance)
        {
            if (!ScienceBirdTweaks.ThickDustClouds.Value && !ScienceBirdTweaks.DustCloudsNoise.Value)
            {
                return;
            }
            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds)
            {
                GameObject dustClouds = __instance.effects[0].effectObject;
                if (dustClouds != null && (!dustClouds.activeInHierarchy || !__instance.effects[0].effectEnabled) && GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isInsideFactory)
                {
                    ScienceBirdTweaks.Logger.LogDebug("DOING OVERRIDE SET ACTIVE");
                    dustClouds.SetActive(true);
                    __instance.effects[0].effectEnabled = true;
                }
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Start))]
        [HarmonyPostfix]
        static void SetInitial(TimeOfDay __instance)
        {
            if (!ScienceBirdTweaks.ThickDustClouds.Value && !ScienceBirdTweaks.DustCloudsNoise.Value)
            {
                return;
            }
            initialSet = true;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetWeatherEffects))]
        [HarmonyPrefix]
        static void CheckBefore(TimeOfDay __instance)
        {
            if (!ScienceBirdTweaks.ThickDustClouds.Value && !ScienceBirdTweaks.DustCloudsNoise.Value)
            {
                return;
            }
            GameObject dustClouds = TimeOfDay.Instance.effects[0].effectObject;
            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds && dustClouds != null)
            {
                enableBuffer = __instance.effects[0].effectEnabled;
                transitionBuffer = __instance.effects[0].transitioning;
                __instance.effects[0].effectEnabled = false;
                __instance.effects[0].transitioning = transitionOverride;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetWeatherEffects))]
        [HarmonyPostfix]
        static void ChangeEffectObject(TimeOfDay __instance)
        {
            if (!ScienceBirdTweaks.ThickDustClouds.Value && !ScienceBirdTweaks.DustCloudsNoise.Value)
            {
                return;
            }
            GameObject dustClouds = TimeOfDay.Instance.effects[0].effectObject;
            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds && dustClouds != null)
            {
                __instance.effects[0].effectEnabled = enableBuffer;
                __instance.effects[0].transitioning = transitionBuffer;
                transitionOverride = true;
                if (initialSet)// one-time setup of cloud thickness and audio
                {
                    enableBuffer = true;
                    __instance.effects[0].lerpPosition = false;
                    GameObject cloudsAmbience = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("DustCloudsAmbience");
                    if (cloudsAmbience != null && dustClouds != null)
                    {
                        LocalVolumetricFog clouds = dustClouds.GetComponent<LocalVolumetricFog>();
                        if (clouds != null && ScienceBirdTweaks.ThickDustClouds.Value)
                        {
                            clouds.parameters.meanFreePath = ScienceBirdTweaks.DustCloudsThickness.Value;
                        }
                        if (ScienceBirdTweaks.DustCloudsNoise.Value)
                        {
                            if (!dustClouds.GetComponentInChildren<AudioSource>())
                            {
                                GameObject audioObj = Object.Instantiate(cloudsAmbience, Vector3.zero, Quaternion.identity);
                                audioObj.transform.SetParent(dustClouds.transform, worldPositionStays: false);
                            }
                            AudioSource cloudsAudio = dustClouds.GetComponentInChildren<AudioSource>();
                            if (cloudsAudio != null)
                            {
                                cloudsAudio.Play();
                            }
                            else
                            {
                                ScienceBirdTweaks.Logger.LogError("Null dust clouds audio!");
                            }
                        }
                    }
                    initialSet = false;
                }
                if (__instance.effects[0].effectEnabled && GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isInsideFactory)// essentially a replacement of existing SetWeatherEffects logic for Dust Clouds
                {
                    __instance.effects[0].transitioning = false;
                    if (__instance.effects[0].effectObject != null)
                    {
                        Vector3 vector = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead) ? StartOfRound.Instance.localPlayerController.transform.position : StartOfRound.Instance.spectateCamera.transform.position);
                        vector += Vector3.up * 4f;
                        dustClouds.transform.position = vector;
                    }
                }
                else if (!__instance.effects[0].transitioning)
                {
                    transitionOverride = false;
                }
            }
            else if (dustClouds != null && dustClouds.activeInHierarchy)// case for Experimentation dust clouds, which aren't an actual weather. they use all the vanilla values/logic
            {
                ScienceBirdTweaks.Logger.LogDebug("EXP DUST CLOUDS");
                AudioSource cloudsAudio = dustClouds.GetComponentInChildren<AudioSource>();
                if (cloudsAudio != null)
                {
                    cloudsAudio.Stop();
                }
                LocalVolumetricFog clouds = dustClouds.GetComponent<LocalVolumetricFog>();
                if (clouds != null)
                {
                    clouds.parameters.meanFreePath = 17f;
                }
                __instance.effects[0].lerpPosition = true;
            }
        }
    }

    [HarmonyPatch]
    public class DustSpaceClouds
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetMapScreenInfoToCurrentLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void AddSpaceToMapScreen(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DustSpaceClouds.Value)
            {
                return;
            }
            string levelText = __instance.screenLevelDescription.text;
            if (levelText.Contains("DustClouds"))
            {
                levelText = levelText.Replace("DustClouds", "Dust Clouds");
                levelText = levelText.Replace("DustyClouds", "Dusty Clouds");// I saw some map descriptions used this
                __instance.screenLevelDescription.text = levelText;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void AddSpaceToTerminal(Terminal __instance)
        {
            if (!ScienceBirdTweaks.DustSpaceClouds.Value)
            {
                return;
            }
            if (__instance.currentText.Contains("DustClouds"))
            {
                __instance.currentText = __instance.currentText.Replace("DustClouds", "Dust Clouds");
                __instance.currentText = __instance.currentText.Replace("DustyClouds", "Dusty Clouds");
                __instance.screenText.text = __instance.currentText;
            }
        }
    }
}
