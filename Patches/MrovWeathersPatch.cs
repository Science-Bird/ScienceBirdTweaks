using System.Reflection;
using System;
using HarmonyLib;
using UnityEngine;
using WeatherRegistry;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine.UIElements;


namespace ScienceBirdTweaks.Patches
{
    public class MrovWeathersPatch
    {
        public static void DoPatching()
        {
            MethodInfo method1 = typeof(WeatherEffectController).GetMethods().FirstOrDefault(x => x.Name == "SetWeatherEffects" && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(Weather[]));
            MethodInfo method2 = typeof(WeatherEffectController).GetMethods().FirstOrDefault(x => x.Name == "SetWeatherEffects" && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(LevelWeatherType[]));
            MethodInfo method3 = typeof(WeatherEffectController).GetMethods().FirstOrDefault(x => x.Name == "SetWeatherEffects" && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(Weather));

            ScienceBirdTweaks.Harmony?.Patch(method1, postfix: new HarmonyMethod(typeof(TrueBlackoutPatch).GetMethod("OnSetWeathers")));
            ScienceBirdTweaks.Harmony?.Patch(method2, postfix: new HarmonyMethod(typeof(TrueBlackoutPatch).GetMethod("OnSetWeatherTypes")));
            ScienceBirdTweaks.Harmony?.Patch(method3, postfix: new HarmonyMethod(typeof(TrueBlackoutPatch).GetMethod("OnSetWeather")));
        }
    }

    public class TrueBlackoutPatch
    {
        public static void BlackoutOverride()
        {
            ScienceBirdTweaks.Logger.LogInfo("Doing blackout override...");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Scene is not loaded! Aborting!");
                return;
            }

            List<GameObject> lightObjects = new List<GameObject>();
            GameObject[] objectList = Resources.FindObjectsOfTypeAll<GameObject>().ToArray();

            List<GameObject> lightObjectBlacklist = new List<GameObject>();

            foreach (GameObject obj in objectList)
            {
                Light[] childLights = obj.GetComponentsInChildren<Light>(true);
                foreach (Light light in childLights)
                {
                    if (light.bakingOutput.lightmapBakeType ==  LightmapBakeType.Baked)
                    {
                        if (light.transform != null && light.transform.parent != null)
                        {
                            lightObjectBlacklist.Add(light.transform.parent.gameObject);
                        }
                        continue;
                    }
                    GameObject? parent = null;
                    if (light.transform != null)
                    {
                        if (light.transform.parent != null)
                        {
                            parent = light.transform.parent.gameObject;
                        }
                        else
                        {
                            ScienceBirdTweaks.Logger.LogDebug("Null parent!");
                        }
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Null transform!");
                    }
                    if (parent != null && !lightObjects.Contains(parent))
                    {
                        lightObjects.Add(parent);
                    }
                    else if (parent == null)
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Couldn't find parent! Skipping...");
                    }
                }
            }
            if (lightObjects.Count() == 0)
            {
                ScienceBirdTweaks.Logger.LogInfo("Found no lights!");
                return;
            }
            ScienceBirdTweaks.Logger.LogDebug($"Done lights loop! Found {lightObjects.Count()} light objects ({lightObjectBlacklist.Count()} blacklisted).");

            foreach (GameObject lightObject in lightObjects)
            {
                string hierarchyString = string.Join("/", lightObject.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray());
                if (lightObjectBlacklist.Contains(lightObject) || hierarchyString.Contains("HangarShip") || hierarchyString.Contains("PlayersContainer") || hierarchyString.Contains("MaskMesh") || hierarchyString.Contains("EyesFilled") || hierarchyString.Contains("Systems/"))
                {
                    continue;
                }
                else if (hierarchyString.IsNullOrWhiteSpace())
                {
                    continue;
                }
                Renderer[] lightRenderers = lightObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in lightRenderers)
                {
                    if (renderer.gameObject != lightObject)
                    {
                        hierarchyString = string.Join("/", renderer.gameObject.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray());
                    }
                    Material[] rMaterials = renderer.materials;
                    for (int i = 0; i < rMaterials.Length; i++)
                    {
                        if (rMaterials[i] != null)
                        {
                            if (renderer.materials[i].HasProperty("_EmissionMap") || (renderer.materials[i].HasProperty("_UseEmissiveIntensity") && renderer.materials[i].GetFloat("_UseEmissiveIntensity") == 1f) || renderer.materials[i].IsKeywordEnabled("_EMISSIVE_COLOR_MAP"))
                            {
                                if (!hierarchyString.IsNullOrWhiteSpace())
                                {
                                    GameObject targetObj = GameObject.Find(hierarchyString);
                                    if (targetObj != null)
                                    {
                                        //ScienceBirdTweaks.Logger.LogDebug($"Darkening the material {rMaterials[i].name} of {renderer.gameObject.name} ({hierarchyString}).");
                                        rMaterials[i].SetColor("_EmissionColor", new Color(0f, 0f, 0f, 1f));
                                        rMaterials[i].SetColor("_EmissiveColor", new Color(0f, 0f, 0f, 1f));
                                    }
                                    else
                                    {
                                        ScienceBirdTweaks.Logger.LogDebug($"Couldn't find object with string {hierarchyString}!");
                                    }
                                }
                                else
                                {
                                    ScienceBirdTweaks.Logger.LogDebug($"Null hierarchyString! Name: {renderer.gameObject.name}");
                                }
                            }
                        }
                        else
                        {
                            ScienceBirdTweaks.Logger.LogDebug("Null material!");
                        }
                    }
                    renderer.materials = rMaterials;
                }
            }
        }

        public static void OnSetWeathers(Weather[] weathers)
        {
            bool foundBlackout = false;
            foreach (Weather weather in weathers)
            {
                if (weather != null)
                {
                    if (weather.Name == "Blackout")
                    {
                        foundBlackout = true;
                        break;
                    }
                }
            }
            if (foundBlackout)
            {
                BlackoutOverride();
            }
        }

        public static void OnSetWeatherTypes(LevelWeatherType[] weatherTypes)
        {
            bool foundBlackout = false;
            foreach (LevelWeatherType weatherType in weatherTypes)
            {
                Weather weather = WeatherManager.GetWeather(weatherType);
                if (weather != null)
                {
                    if (weather.Name == "Blackout")
                    {
                        foundBlackout = true;
                        break;
                    }
                }
            }
            if (foundBlackout)
            {
                BlackoutOverride();
            }
        }

        public static void OnSetWeather(Weather weather)
        {
            if (weather != null)
            {
                if (weather.Name == "Blackout")
                {
                    BlackoutOverride();
                }
            }
        }
    }
}
