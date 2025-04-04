using System.Reflection;
using System;
using HarmonyLib;
using UnityEngine;
using WeatherRegistry;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using System.Diagnostics;
using System.Text;
using static UnityEngine.UI.Image;
using System.Collections;
using UnityEngine.Rendering.HighDefinition;
using MrovWeathers;


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
                if (onEnable != null && customMethod != null)
                {
                    ScienceBirdTweaks.Harmony.Patch(onEnable, prefix: new HarmonyMethod(customMethod));
                }
            }
        }
    }

    public static class TrueBlackoutPatch
    {
        private static readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int emissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private static readonly int useEmissiveIntensityID = Shader.PropertyToID("_UseEmissiveIntensity");
        private static readonly int emissionMapID = Shader.PropertyToID("_EmissionMap");
        private static readonly int surfaceTypeID = Shader.PropertyToID("_SurfaceType");
        private const string emissiveColorMapKeyword = "_EMISSIVE_COLOR_MAP";

        public static bool BlackoutOverridePrefix(object __instance)
        {
            BlackoutOverride();

            return false;
        }

        private static void BlackoutOverride()
        {
            var totalStopwatch = Stopwatch.StartNew();
            var extraLogs = false;

            ScienceBirdTweaks.Logger.LogInfo("Doing blackout override...");
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.isLoaded)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Scene is not loaded! Aborting!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL BlackoutOverride execution time (aborted): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Light component search: {totalStopwatch.ElapsedMilliseconds}ms");

            //Light[] allLights = GameObject.FindObjectsOfType<Light>(true); // My method
            List<Light> allLights = Blackout.LightUtils.GetLightsInScene(StartOfRound.Instance.currentLevel.sceneName); // Mrov's method

            //List<Light> allLights = new List<Light>(); // Merge of the two
            //allLights.AddRange(GameObject.FindObjectsOfType<Light>(true));
            //allLights.AddRange(Blackout.LightUtils.GetLightsInScene(StartOfRound.Instance.currentLevel.sceneName));
            //allLights = allLights.Distinct().ToList();

            HashSet<GameObject> lightObjects = new HashSet<GameObject>();
            HashSet<GameObject> lightObjectsBlacklist = new HashSet<GameObject>();

            Material blackMaterial = new Material(Shader.Find("HDRP/Lit"));
            blackMaterial.color = Color.black;
            blackMaterial.SetFloat(surfaceTypeID, 0);
            blackMaterial.DisableKeyword(emissiveColorMapKeyword);

            static bool FastContains(string path, string keyword) =>
                path.IndexOf(keyword, StringComparison.Ordinal) >= 0;

            foreach (Light light in allLights)
            {
                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Found light {light.name} in {GetObjectPath(light.transform.parent.gameObject)} with type {light.type} and bake type {light.bakingOutput.lightmapBakeType}");

                Transform? parent = light.transform.parent;

                if (parent == null)
                    continue;

                if (lightObjectsBlacklist.Contains(parent.gameObject))
                    continue;                    

                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked || parent.name == "BlackoutIgnore" || HasParentWithInteractTrigger(light.gameObject))
                {
                    lightObjectsBlacklist.Add(parent.gameObject);

                    if (lightObjects.Contains(parent.gameObject))
                        lightObjects.Remove(parent.gameObject);

                    continue;
                }

                string path = GetObjectPath(parent.gameObject);

                // TODO: pull these from a config file
                if (FastContains(path, "HangarShip") ||
                    FastContains(path, "PlayersContainer") ||
                    FastContains(path, "MaskMesh") ||
                    FastContains(path, "EyesFilled") ||
                    FastContains(path, "GunBody") ||
                    FastContains(path, "Landmine") ||
                    FastContains(path, "Trap") ||
                    (FastContains(path, "Door") && !FastContains(path, "FireExit")) ||
                    //FastContains(parent.name, "Entrance") ||
                    FastContains(parent.name, "Apparatus"))
                    continue;

                lightObjects.Add(parent.gameObject);
            }

            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Light processing: {totalStopwatch.ElapsedMilliseconds}ms");
            ScienceBirdTweaks.Logger.LogDebug($"Found {lightObjects.Count} light objects to process");

            if (lightObjects.Count() == 0)
            {
                ScienceBirdTweaks.Logger.LogInfo("Found no lights!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL BlackoutOverride execution time (no lights found): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            Color zeroEmission = new Color(0f, 0f, 0f, 1f);
            Color black = new Color(0f, 0f, 0f, 1f);

            foreach (GameObject lightObject in lightObjects)
            {
                foreach (Light light in lightObject.GetComponentsInChildren<Light>(true))
                    light.enabled = false;

                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Disabling light object {lightObject.name} with path {GetObjectPath(lightObject)}");

                Renderer[] lightRenderers = lightObject.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in lightRenderers)
                {
                    Material[] materials = renderer.materials;
                    bool materialsModified = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material mat = materials[i];
                        if (mat == null) continue;

                        bool hasEmissionMap = mat.HasProperty(emissionMapID);
                        bool hasUseEmissiveIntensity = mat.HasProperty(useEmissiveIntensityID);
                        float emissiveIntensity = hasUseEmissiveIntensity ? mat.GetFloat(useEmissiveIntensityID) : 0f;
                        bool isEmissiveKeywordEnabled = mat.IsKeywordEnabled(emissiveColorMapKeyword);

                        bool hasEmission = hasEmissionMap || (hasUseEmissiveIntensity && emissiveIntensity > 0) || isEmissiveKeywordEnabled;

                        if (hasEmission)
                        {
                            if (mat.HasProperty(emissionColorID))
                                mat.SetColor(emissionColorID, zeroEmission);

                            if (mat.HasProperty(emissiveColorID))
                                mat.SetColor(emissiveColorID, zeroEmission);

                            if (hasUseEmissiveIntensity)
                                mat.SetFloat(useEmissiveIntensityID, 0);

                            materialsModified = true;

                            if (extraLogs)
                                ScienceBirdTweaks.Logger.LogDebug($"Darkened material {mat.name} of {renderer.gameObject.name}");
                        }
                        else
                        {
                            if (extraLogs)
                                ScienceBirdTweaks.Logger.LogDebug($"Skipping material {mat.name} of {renderer.gameObject.name}");

                            if (mat.name == "SkyEmissive (Instance)" || mat.name.Contains("LEDLight (Instance)")) // Probably look for if an HDRP/lit material has emission / is above a certain brightness
                            {
                                if (extraLogs)
                                    ScienceBirdTweaks.Logger.LogDebug($"Found LED light material {mat.name} of {renderer.gameObject.name}");

                                materials[i] = blackMaterial;
                                materialsModified = true;
                            }
                        }
                    }

                    if (materialsModified)
                        renderer.materials = materials;
                }
            }

            RoundManager.Instance.SwitchPower(on: true);
            RoundManager.Instance.TurnOnAllLights(on: false);
            BreakerBox currentBreakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
            if (currentBreakerBox != null)
                currentBreakerBox.gameObject.SetActive(false);

            UnityEngine.GameObject sun = UnityEngine.GameObject.Find("Sun");
            if (sun != null)
                sun.SetActive(false);

            totalStopwatch.Stop();
            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL BlackoutOverride execution time: {totalStopwatch.ElapsedMilliseconds}ms");
        }

        private static string GetObjectPath(GameObject obj)
        {
            StringBuilder path = new StringBuilder(obj.name);
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }

        private static bool HasParentWithInteractTrigger(GameObject obj)
        {
            Transform current = obj.transform;

            while (current != null)
            {
                if (current.GetComponent<InteractTrigger>() != null)
                {
                    ScienceBirdTweaks.Logger.LogDebug($"Found InteractTrigger in {current.name} with path {GetObjectPath(current.gameObject)}");
                    return true;
                }
                   
                current = current.parent;
            }

            return false;
        }
    }
}
