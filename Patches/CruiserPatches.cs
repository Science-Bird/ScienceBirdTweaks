using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class CruiserPatches
    {
        [HarmonyPatch(typeof(ClipboardItem), nameof(ClipboardItem.Update))]
        [HarmonyPostfix]
        static void UpdatePatch(ClipboardItem __instance)
        {
            if (ScienceBirdTweaks.RemoveCruiserClipboard.Value && __instance.truckManual)
            {
                ScienceBirdTweaks.Logger.LogDebug($"Removing cruiser clipboard!");
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
        }


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void CruiserPrefabPatch(GameNetworkManager __instance)
        {
            if (!ScienceBirdTweaks.SmokeFix.Value) { return; }

            Material smokeParticleMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SmokeParticle");
            VehicleController[] vehicles = UnityEngine.Resources.FindObjectsOfTypeAll<VehicleController>();
            foreach (VehicleController vehicle in vehicles)
            {
                ParticleSystem[] particleSystems = vehicle.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.material != null && (renderer.material.name == "Default-Particle (Instance)" || renderer.material.name == "Default-ParticleSystem (Instance)"))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Found cruiser target particle!");
                        renderer.material = smokeParticleMat;
                    }
                }
            }
        }
    }
}
