using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScienceBirdTweaks.Scripts
{
    public class FullDark : MonoBehaviour
    {
        private static readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int emissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private static readonly int useEmissiveIntensityID = Shader.PropertyToID("_UseEmissiveIntensity");
        private static readonly int emissionMapID = Shader.PropertyToID("_EmissionMap");
        private static readonly int surfaceTypeID = Shader.PropertyToID("_SurfaceType");
        private const string emissiveColorMapKeyword = "_EMISSIVE_COLOR_MAP";
        private static Boolean extraLogs = false;

        public static void DoFullDark(Boolean? disableSun)
        {
            var totalStopwatch = Stopwatch.StartNew();

            ScienceBirdTweaks.Logger.LogInfo("Doing FullDark");
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.isLoaded)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Scene is not loaded! Aborting!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL FullDark execution time (aborted): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            FullDark fullDarkInstance = GameObject.FindObjectOfType<FullDark>();
            if (fullDarkInstance == null)
            {
                GameObject darkObject = new GameObject("FullDarkManager");
                fullDarkInstance = darkObject.AddComponent<FullDark>();
                ScienceBirdTweaks.Logger.LogInfo("Created new FullDark instance");
            }

            List<Light> allLights;
            try
            {
                if (StartOfRound.Instance == null || StartOfRound.Instance.currentLevel == null)
                {
                    ScienceBirdTweaks.Logger.LogWarning("StartOfRound.Instance or currentLevel is null, falling back to all scene lights");
                    allLights = new List<Light>(GameObject.FindObjectsOfType<Light>(true));
                }
                else
                {
                    allLights = GetSceneLights(StartOfRound.Instance.currentLevel.sceneName);
                }
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Error getting scene lights: {ex.Message}. Falling back to all lights.");
                allLights = new List<Light>(GameObject.FindObjectsOfType<Light>(true));
            }

            HashSet<GameObject> lightObjects = new HashSet<GameObject>();
            HashSet<GameObject> lightObjectsBlacklist = new HashSet<GameObject>();
            
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

                string path = GetObjectPath(parent.gameObject);

                // TODO: pull these from a config file
                if (FastContains(path, "HangarShip") ||
                    FastContains(path, "PlayersContainer") ||
                    FastContains(path, "MaskMesh") ||
                    FastContains(path, "EyesFilled") ||
                    FastContains(path, "GunBody") ||
                    FastContains(path, "Landmine") ||
                    FastContains(path, "Trap"))
                {
                    lightObjectsBlacklist.Add(parent.gameObject);

                    if (lightObjects.Contains(parent.gameObject))
                        lightObjects.Remove(parent.gameObject);

                    continue;
                }

                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked ||
                    parent.name == "BlackoutIgnore" ||
                    HasParentWithInteractTrigger(light.gameObject) ||
                    parent.GetComponentInChildren<LungProp>(true) != null)
                {
                    lightObjectsBlacklist.Add(parent.gameObject);

                    if (lightObjects.Contains(parent.gameObject))
                        lightObjects.Remove(parent.gameObject);

                    continue;
                }

                

                lightObjects.Add(parent.gameObject);
            }

            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Light processing finished at {totalStopwatch.ElapsedMilliseconds}ms");
            ScienceBirdTweaks.Logger.LogDebug($"Found {lightObjects.Count} light objects to process");

            if (lightObjects.Count() == 0)
            {
                ScienceBirdTweaks.Logger.LogInfo("Found no lights!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL FullDark execution time (no lights found): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            if (RoundManager.Instance != null)
            {
                try
                {
                    RoundManager.Instance.SwitchPower(on: true);
                    RoundManager.Instance.TurnOnAllLights(on: false);
                }
                catch (Exception ex)
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Error switching power state: {ex.Message}");
                }
            }
            else
            {
                ScienceBirdTweaks.Logger.LogWarning("RoundManager.Instance is null, skipping power switching");
            }

            try
            {
                BreakerBox currentBreakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
                if (currentBreakerBox != null)
                    currentBreakerBox.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Error handling breaker box: {ex.Message}");
            }

            if (disableSun == true)
            {
                try
                {
                    UnityEngine.GameObject sun = UnityEngine.GameObject.Find("Sun");
                    if (sun != null)
                        sun.SetActive(false);
                }
                catch (Exception ex)
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Error disabling sun: {ex.Message}");
                }
            }

            fullDarkInstance.StartCoroutine(fullDarkInstance.DisableLightsOverFrames(lightObjects));

            totalStopwatch.Stop();
            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Material Assignment finished at {totalStopwatch.ElapsedMilliseconds}ms");
        }

        IEnumerator DisableLightsOverFrames(IEnumerable<GameObject> lightObjectsEnumerable)
        {
            var lightObjects = lightObjectsEnumerable.ToList();
            int batchSize = 10;
            int index = 0;
            int framesToWait = 25;

            WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

            Color zeroEmission = new Color(0f, 0f, 0f, 1f);

            Material blackMaterial = new Material(Shader.Find("HDRP/Lit"));
            blackMaterial.color = Color.black;
            blackMaterial.SetFloat(surfaceTypeID, 0);
            blackMaterial.DisableKeyword(emissiveColorMapKeyword);

            while (index < lightObjects.Count)
            {
                for (int i = 0; i < batchSize && index < lightObjects.Count; i++, index++)
                {
                    GameObject lightObject = lightObjects[index];

                    foreach (Light light in lightObject.GetComponentsInChildren<Light>(true))
                        light.enabled = false;

                    if (extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Disabling light object {lightObject.name} with path {GetObjectPath(lightObject)}");

                    foreach (Renderer renderer in lightObject.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.materials;
                        bool materialsModified = false;

                        for (int j = 0; j < materials.Length; j++)
                        {
                            Material mat = materials[j];
                            if (mat == null) continue;

                            bool hasEmissionMap = mat.HasProperty(emissionMapID);
                            bool hasEmissionColor = mat.HasProperty(emissionColorID);
                            bool hasEmissiveColor = mat.HasProperty(emissiveColorID);
                            bool hasUseEmissiveIntensity = mat.HasProperty(useEmissiveIntensityID);
                            float emissiveIntensity = hasUseEmissiveIntensity ? mat.GetFloat(useEmissiveIntensityID) : 0f;
                            bool isEmissiveKeywordEnabled = mat.IsKeywordEnabled(emissiveColorMapKeyword);

                            bool hasEmission = hasEmissionMap || (hasUseEmissiveIntensity && emissiveIntensity > 0) || isEmissiveKeywordEnabled;

                            if (hasEmission)
                            {
                                if (hasEmissionMap)
                                    mat.SetColor(emissionColorID, zeroEmission);
                                if (hasEmissionColor)
                                    mat.SetColor(emissiveColorID, zeroEmission);

                                materialsModified = true;

                                if (extraLogs)
                                    ScienceBirdTweaks.Logger.LogDebug($"Darkened material {mat.name} of {renderer.gameObject.name}");
                            }
                            else
                            {
                                if (extraLogs)
                                    ScienceBirdTweaks.Logger.LogDebug($"Skipping material {mat.name} of {renderer.gameObject.name}");

                                if (mat.name == "SkyEmissive (Instance)" || mat.name.Contains("LEDLight (Instance)"))
                                {
                                    if (extraLogs)
                                        ScienceBirdTweaks.Logger.LogDebug($"Found LED light material {mat.name} of {renderer.gameObject.name}");

                                    materials[j] = blackMaterial;
                                    
                                    materialsModified = true;
                                }
                            }
                        }

                        if (materialsModified)
                            renderer.materials = materials;
                    }
                }
                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Processed {index} light objects, sleeping for a frame");

                for (int f = 0; f < framesToWait; f++)
                    yield return waitForEndOfFrame;
            }
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

        public static List<Light> GetSceneLights(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            var lights = new List<Light>();

            foreach (var rootObj in scene.GetRootGameObjects())
            {
                lights.AddRange(rootObj.GetComponentsInChildren<Light>(true));
            }

            return lights;
        }
    }
}
