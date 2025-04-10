using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using static MrovWeathers.Blackout;

namespace ScienceBirdTweaks.Scripts
{
    public class FullDark : MonoBehaviour
    {
        private static readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int emissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private static readonly int emissiveIntensityID = Shader.PropertyToID("_EmissiveIntensity");
        private static readonly int emissionMapID = Shader.PropertyToID("_EmissionMap");
        private static readonly int surfaceTypeID = Shader.PropertyToID("_SurfaceType");
        private const string emissiveColorMapKeyword = "_EMISSIVE_COLOR_MAP";
        private const string useEmissiveIntensityID = "_EMISSION";
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
                allLights = GetSceneLights(StartOfRound.Instance.currentLevel.sceneName);

                if (allLights == null || allLights.Count == 0)
                    throw new Exception("GetSceneLights found no lights");
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Error getting scene lights: {ex.Message}. Falling back to all of Type<Light>.");
                allLights = new List<Light>(GameObject.FindObjectsOfType<Light>(true));
            }

            if (allLights == null || allLights.Count == 0)
            {
                ScienceBirdTweaks.Logger.LogWarning("No lights found in scene!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL FullDark execution time (no lights found): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            HashSet<GameObject> lightObjects = new HashSet<GameObject>();
            HashSet<GameObject> lightObjectsBlacklist = new HashSet<GameObject>();

            float floodLightIntensity = ScienceBirdTweaks.FloodLightIntensity.Value;
            float floodLightAngle = ScienceBirdTweaks.FloodLightIntensity.Value;
            float floodLightRange = ScienceBirdTweaks.FloodLightIntensity.Value;

            List<HDAdditionalLightData> Floodlights = new List<HDAdditionalLightData>();
            Transform ShipLightsPost = GameObject.Find("ShipLightsPost").GetComponent<Transform>();
            List<Light> ShipPostLights = LightUtils.GetLightsUnderParent(ShipLightsPost);

            ScienceBirdTweaks.Logger.LogInfo($"Found {ShipPostLights.Count} floodlights in scene");

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

                //if (floodLightIntensity <= 0 && ShipPostLights.Contains(light))
                //{
                //    ScienceBirdTweaks.Logger.LogDebug($"Floodlight intensity is 0, adding floodlight {light.name} to whitelist");
                //    lightObjects.Add(parent.gameObject);
                //    continue;
                //}

                string path = GetObjectPath(parent.gameObject);

                // TODO: pull these from a config file
                if ((FastContains(path, "HangarShip")) ||
                    FastContains(path, "PlayersContainer") ||
                    FastContains(path, "MaskMesh") ||
                    FastContains(path, "EyesFilled") ||
                    FastContains(path, "GunBody") ||
                    FastContains(path, "Landmine") ||
                    FastContains(path, "Trap"))
                {
                    lightObjectsBlacklist.Add(parent.gameObject);

                    if (extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Skipping light object {parent.gameObject.name} with path {path} due to blacklist");

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
                    if (extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Skipping light object {parent.gameObject.name} with path {path} due to baked lightmap or interact trigger");

                    if (lightObjects.Contains(parent.gameObject))
                        lightObjects.Remove(parent.gameObject);

                    continue;
                }

                lightObjects.Add(parent.gameObject);
            }

            if (floodLightIntensity <= 0)
            {
                foreach (Light light in ShipPostLights)
                {
                    lightObjects.Add(light.transform.parent.gameObject);
                }
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
                try
                {
                    foreach (Light light in ShipPostLights)
                    {
                        light.gameObject.TryGetComponent<HDAdditionalLightData>(out var FloodlightData);

                        if (FloodlightData != null)
                        {
                            if (floodLightIntensity > 0)
                            {
                                FloodlightData.SetIntensity(floodLightIntensity);
                                FloodlightData.SetSpotAngle(floodLightAngle);
                                FloodlightData.SetRange(floodLightRange);
                                    
                            }
                            else
                            {
                                FloodlightData.SetIntensity(0);
                                FloodlightData.SetSpotAngle(0);
                                FloodlightData.SetRange(0);
                            }

                            Floodlights.Add(FloodlightData);
                        }
                    }
                }
                catch (Exception arg)
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Error while trying to modify floodlights: {arg}");
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
                    {
                        if (extraLogs)
                            ScienceBirdTweaks.Logger.LogDebug($"Disabling light {light.name} with path {GetObjectPath(lightObject)}");

                        light.enabled = false;
                    }

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
                            bool hasEmissiveIntensity = mat.HasProperty(emissiveIntensityID);
                            float currentIntensity = hasEmissiveIntensity ? mat.GetFloat(emissiveIntensityID) : 0f;
                            bool isEmissiveKeywordEnabled = mat.IsKeywordEnabled(emissiveColorMapKeyword);

                            bool hasEmission = hasEmissionMap || hasEmissionColor || hasEmissiveColor || hasEmissiveIntensity || isEmissiveKeywordEnabled;

                            if (hasEmission)
                            {
                                if (hasEmissionColor)
                                    mat.SetColor(emissionColorID, zeroEmission);
                                if (hasEmissiveColor)
                                    mat.SetColor(emissiveColorID, zeroEmission);
                                if (hasEmissiveIntensity)
                                    mat.SetFloat(emissiveIntensityID, 0f);

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
