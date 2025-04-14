using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using ScienceBirdTweaks.Scripts;
using System.IO;

namespace ScienceBirdTweaks.Scripts
{
    public static class CollectionExtensions
    {
        private static System.Random rng = new System.Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);

                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class TrueBlackout : MonoBehaviour
    {
        private static readonly int emissiveColorID = Shader.PropertyToID("_EmissiveColor");
        private static readonly int emissionMapID = Shader.PropertyToID("_EmissionMap");
        private static readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int emissiveIntensityID = Shader.PropertyToID("_EmissiveIntensity");
        private static readonly int surfaceTypeID = Shader.PropertyToID("_SurfaceType");
        private const string emissiveColorMapKeyword = "_EMISSIVE_COLOR_MAP";
        private static readonly Boolean extraLogs = ScienceBirdTweaks.ExtraLogs.Value;
        private static string[] nameBlacklist = InitializeBlacklist(ScienceBirdTweaks.TrueBlackoutNameBlacklist.Value);
        private static readonly int nameBlacklistLength = nameBlacklist.Length;
        private static string[] hierarchyBlacklist = InitializeBlacklist(ScienceBirdTweaks.TrueBlackoutHierarchyBlacklist.Value);
        private static readonly int hierarchyBlacklistLength = hierarchyBlacklist.Length;
        private static readonly Boolean doContainsCheck = hierarchyBlacklistLength > 0;

        private static string[] InitializeBlacklist(string configValue)
        {
            if (string.IsNullOrWhiteSpace(configValue))
                return Array.Empty<string>();

            string cleanedValue = configValue.Replace(" ", "");

            if (string.IsNullOrEmpty(cleanedValue))
                return Array.Empty<string>();

            return cleanedValue.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        static bool FastContains(string path, string keyword) =>
            path.IndexOf(keyword, StringComparison.Ordinal) >= 0;

        static bool BlacklistIsSame(string name)
        {
            for (int i = 0; i < nameBlacklistLength; i++)
            {
                if (name == nameBlacklist[i])
                {
                    return true;
                }
            }
            return false;
        }

        static bool BlacklistContains(string path)
        {
            for (int i = 0; i < hierarchyBlacklistLength; i++)
            {
                if (FastContains(path, hierarchyBlacklist[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static void DoBlackout(Boolean? disableSun)
        {
            var totalStopwatch = Stopwatch.StartNew();

            ScienceBirdTweaks.Logger.LogInfo("Doing blackout!");
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.isLoaded)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Scene is not loaded. Aborting!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL blackout execution time (aborted): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            TrueBlackout trueBlackoutInstance = GameObject.FindObjectOfType<TrueBlackout>();
            if (trueBlackoutInstance == null)
            {
                GameObject blackoutHandler = new GameObject("BlackoutManager");
                trueBlackoutInstance = blackoutHandler.AddComponent<TrueBlackout>();
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
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL TrueBlackout execution time (no lights found): {totalStopwatch.ElapsedMilliseconds}ms");
                return;
            }

            HashSet<GameObject> lightObjects = new HashSet<GameObject>();
            HashSet<GameObject> lightObjectsBlacklist = new HashSet<GameObject>();

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

                if (BlacklistIsSame(parent.gameObject.name) || (doContainsCheck && BlacklistContains(path)))
                {
                    lightObjectsBlacklist.Add(parent.gameObject);

                    if (extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Skipping light object {parent.gameObject.name} with path {path} due to blacklist");

                    if (lightObjects.Contains(parent.gameObject))
                        lightObjects.Remove(parent.gameObject);

                    continue;
                }

                if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked ||
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

                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Adding light object {parent.gameObject.name} with path {path} to processing list");

                lightObjects.Add(parent.gameObject);
            }

            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Light processing finished at {totalStopwatch.ElapsedMilliseconds}ms");
            ScienceBirdTweaks.Logger.LogDebug($"Found {lightObjects.Count} light objects to process");

            if (lightObjects.Count() == 0)
            {
                ScienceBirdTweaks.Logger.LogInfo("Found no lights!");
                totalStopwatch.Stop();
                ScienceBirdTweaks.Logger.LogDebug($"[TIMER] TOTAL Blackout execution time (no lights found): {totalStopwatch.ElapsedMilliseconds}ms");
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
                    ScienceBirdTweaks.Logger.LogDebug($"Setting spotlight lighting for Blackout");

                    ShipFloodlightController shipFloodlightController = GameObject.FindObjectOfType<ShipFloodlightController>();

                    if (shipFloodlightController != null)
                    {
                        shipFloodlightController.SetFloodlightData(
                            ScienceBirdTweaks.BlackoutFloodLightIntensity.Value,
                            ScienceBirdTweaks.BlackoutFloodLightAngle.Value,
                            ScienceBirdTweaks.BlackoutFloodLightRange.Value
                        );
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogWarning("ShipFloodlightController instance not found in the scene.");
                    }
                }
                catch (Exception arg)
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Error while trying to modify floodlights: {arg}");
                }

                GameObject spriteObject = GameObject.Find("LightBehindDoor");

                if (spriteObject != null)
                {
                    SpriteRenderer spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
                    ScienceBirdTweaks.Logger.LogDebug($"Found sprite object {spriteObject.name} with path {GetObjectPath(spriteObject)}");

                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = Color.black;
                        ScienceBirdTweaks.Logger.LogDebug($"Set color of sprite object {spriteObject.name} to black with path {GetObjectPath(spriteObject)}");
                    }
                    else
                        ScienceBirdTweaks.Logger.LogWarning($"SpriteRenderer not found on {spriteObject.name} with path {GetObjectPath(spriteObject)}");
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Sprite object not found in scene");
                }
            }

            trueBlackoutInstance.StartCoroutine(trueBlackoutInstance.DisableLightsOverFrames(lightObjects));

            totalStopwatch.Stop();
            ScienceBirdTweaks.Logger.LogDebug($"[TIMER] Material Assignment finished at {totalStopwatch.ElapsedMilliseconds}ms");
        }

        IEnumerator DisableLightsOverFrames(IEnumerable<GameObject> lightObjectsEnumerable)
        {
            var lightObjects = lightObjectsEnumerable.ToList();

            lightObjects.Shuffle();

            int batchSize = 5;
            int index = 0;
            int framesToWait = 15;

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
                        //if (extraLogs)
                        //    ScienceBirdTweaks.Logger.LogDebug($"Disabling light {light.name} with path {GetObjectPath(lightObject)}");

                        light.enabled = false;
                    }

                    //if (extraLogs)
                    //    ScienceBirdTweaks.Logger.LogDebug($"Disabling light object {lightObject.name} with path {GetObjectPath(lightObject)}");

                    foreach (Renderer renderer in lightObject.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.materials;
                        bool materialsModified = false;

                        for (int j = 0; j < materials.Length; j++)
                        {
                            Material mat = materials[j];
                            if (mat == null) continue;

                            bool hasEmissionMap = mat.HasProperty(emissionMapID);
                            bool hasEmissiveColor = mat.HasProperty(emissiveColorID);
                            bool hasEmissionColor = mat.HasProperty(emissionColorID);
                            bool hasEmissiveIntensity = mat.HasProperty(emissiveIntensityID);
                            bool isEmissiveKeywordEnabled = mat.IsKeywordEnabled(emissiveColorMapKeyword);

                            //if (extraLogs)
                            //    ScienceBirdTweaks.Logger.LogDebug($"Material {mat.name} of {renderer.gameObject.name} has emission: {hasEmission}");

                            if (isEmissiveKeywordEnabled || hasEmissiveColor || hasEmissionColor || hasEmissiveIntensity)
                            {
                                if (hasEmissiveColor)
                                    mat.SetColor(emissiveColorID, zeroEmission);
                                if (hasEmissionColor)
                                    mat.SetColor(emissionColorID, zeroEmission);
                                if (hasEmissiveIntensity)
                                    mat.SetFloat(emissiveIntensityID, 0f);

                                materialsModified = true;

                                //if (extraLogs)
                                //    ScienceBirdTweaks.Logger.LogDebug($"Darkened material {mat.name} of {renderer.gameObject.name}");
                            }
                            else
                            {
                                //if (extraLogs)
                                //    ScienceBirdTweaks.Logger.LogDebug($"Skipping material {mat.name} of {renderer.gameObject.name} which has the following properties: {mat.color}");

                                if (mat.name == "SkyEmissive (Instance)")
                                {
                                    //if (extraLogs)
                                    //    ScienceBirdTweaks.Logger.LogDebug($"Found LED light material {mat.name} of {renderer.gameObject.name}");

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
        
        public static List<Light> GetLightsInParent(Transform parent, bool includeInactive = true)
        {
            List<Light> lights = [];
            if (parent == null)
                return lights;

            Light[] childLights = parent.GetComponentsInChildren<Light>(includeInactive);
            lights.AddRange(childLights);

            return lights;
        }

        public static List<Light> GetSceneLights(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            var lights = new List<Light>();
            var sceneRootObjects = scene.GetRootGameObjects();

            foreach (var rootObj in scene.GetRootGameObjects())
            {
                lights.AddRange(rootObj.GetComponentsInChildren<Light>(true));
            }

            return lights;
        }
    }
}
