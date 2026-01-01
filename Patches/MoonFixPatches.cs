using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class MoonFixes
    {
        public static Material originalQuicksand;
        public static Material aquatisQuicksand;
        public static Material defaultTerrainHD;
        private static bool flag = false;
        private static bool done = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void OnStart(StartOfRound __instance)
        {
            Material[] terrainMats = UnityEngine.Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "DefaultHDTerrainMaterial").ToArray();
            if (terrainMats != null && terrainMats.Length > 0)
            {
                defaultTerrainHD = terrainMats.First();
            }
            if (ScienceBirdTweaks.ClientsideMode.Value) { return; }
            aquatisQuicksand = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("AquatisQuicksandTexture");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void OnLoad(StartOfRound __instance, string sceneName)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value) { return; }
            if (originalQuicksand == null && RoundManager.Instance != null)
            {
                DecalProjector decal = RoundManager.Instance.quicksandPrefab.GetComponentInChildren<DecalProjector>();
                originalQuicksand = decal.material;
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        static void ChangeOverride(RoundManager __instance)// specifically goes after LLL quicksand override patch
        {
            if (ScienceBirdTweaks.ClientsideMode.Value) { return; }
            if (StartOfRound.Instance.currentLevel.PlanetName == "112 Aquatis" || StartOfRound.Instance.currentLevel.PlanetName == "2 Ganimedes" || StartOfRound.Instance.currentLevel.PlanetName == "43 Orion")
            {
                flag = true;
                DecalProjector decal = __instance.quicksandPrefab.GetComponentInChildren<DecalProjector>();
                decal.material = aquatisQuicksand;
            }

        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
        [HarmonyPrefix]
        static void TerrainFix(RoundManager __instance)
        {
            if (!done)
            {
                done = true;
                if (StartOfRound.Instance.currentLevel.PlanetName == "2 Ganimedes" && defaultTerrainHD != null)// replace material with bugged shader to regular unity default
                {
                    Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
                    for (int i = 0; i < terrains.Length; i++)
                    {
                        terrains[i].materialTemplate = defaultTerrainHD;
                        terrains[i].renderingLayerMask = 1797;
                    }
                }
                else if (StartOfRound.Instance.currentLevel.PlanetName == "85 Rend")
                {
                    GameObject terrain = GameObject.Find("Environment/CompletedRendTerrain");
                    if (terrain != null && terrain.GetComponent<MeshRenderer>())
                    {
                        terrain.GetComponent<MeshRenderer>().renderingLayerMask = 1797;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(animatedSun), nameof(animatedSun.Start))]
        [HarmonyPostfix]
        static void OnStart(animatedSun __instance)
        {
            done = false;
            if (ScienceBirdTweaks.ForceSunShadows.Value && TimeOfDay.Instance != null)
            {
                __instance.directLight.shadows = LightShadows.Hard;
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPrefix]
        static void EndOfRound(RoundManager __instance)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value) { return; }
            if (flag)
            {
                DecalProjector decal = __instance.quicksandPrefab.GetComponentInChildren<DecalProjector>();
                decal.material = originalQuicksand;
                flag = false;
            }
        }
    }
}

