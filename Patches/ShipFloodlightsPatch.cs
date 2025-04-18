using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Unity.Netcode.Components;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class ShipFloodlightsPatch
    {
        private static bool spinnerAdded = false;
        public static GameObject interactPrefab;
        private static bool nullHandler = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeInteractPrefab()
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && ScienceBirdTweaks.FloodlightRotation.Value)
            {
                ScienceBirdTweaks.Logger.LogDebug("Initializing rotation interact!");
                interactPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("ShipButtonInteract");
                NetworkManager.Singleton.AddNetworkPrefab(interactPrefab);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ReplaceButtonPanel(StartOfRound __instance, string sceneName)// load new button interaction
        {
            if (!ScienceBirdTweaks.FloodlightRotation.Value || interactPrefab == null)
            {
                return;
            }

            if (sceneName == "SampleSceneRelay" && !GameObject.Find("ShipButtonInteract(Clone)"))// if loading into ship and interact not already present
            {
                ScienceBirdTweaks.Logger.LogDebug("Spawning new button panel...");
                GameObject buttonPanel = GameObject.Find("Environment/HangarShip/ControlPanelWTexture");
                GameObject ship = GameObject.Find("Environment/HangarShip");
                if (buttonPanel == null || ship == null) { return; }

                if (__instance.IsServer)
                {
                    GameObject interact = Object.Instantiate(interactPrefab, buttonPanel.transform.position, Quaternion.identity);
                    interact.GetComponent<NetworkObject>().Spawn();
                    if (interact.GetComponent<NetworkObject>().TrySetParent(ship.transform, worldPositionStays: true))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Parented button group to ship!");
                    }
                }
                GameObject newPanel = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("NewButtonPanel");
                MeshFilter newPanelMesh = newPanel.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh.name == "ControlPanelWTexture").First();
                if (newPanelMesh != null)
                {
                    buttonPanel.GetComponent<MeshFilter>().sharedMesh = newPanelMesh.sharedMesh;
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogError("Couldn't find new panel!");
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPostfix]
        static void OnShipLand(StartOfRound __instance)// start spinning on ship land, since ship landed bool is only set right after this, it will just queue up the spin
        {
            if (ScienceBirdTweaks.FloodlightRotation.Value && ScienceBirdTweaks.FloodlightRotationOnLand.Value && spinnerAdded)
            {
                ShipFloodlightController floodlightController = Object.FindObjectOfType<ShipFloodlightController>();
                if (floodlightController != null)
                {
                    floodlightController.StartSpinning();
                }
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void AddSpinnerComponentPatch(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.FloodlightRotation.Value) { return; }

            if (!spinnerAdded && __instance.gameObject.GetComponent<ShipFloodlightController>() == null)
            {
                __instance.gameObject.AddComponent<ShipFloodlightController>();
                spinnerAdded = true;
                ScienceBirdTweaks.Logger.LogInfo("ShipFloodlightController component added.");
            }
            else if (spinnerAdded)
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController already added, skipping.");
            }
            else if (__instance.gameObject.GetComponent<ShipFloodlightController>() != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController component already exists on StartOfRound object but wasn't tracked. Marking as added.");
                spinnerAdded = true;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix]
        static void ResetSpinnerFlagPatch()
        {
            if (!ScienceBirdTweaks.FloodlightRotation.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug("Disconnect detected, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "OnDestroy")]
        [HarmonyPostfix]
        static void OnDestroyResetSpinnerFlagPatch()
        {
            if (!ScienceBirdTweaks.FloodlightRotation.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug("StartOfRound OnDestroy, resetting spinnerAdded flag.");
            spinnerAdded = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "ReviveDeadPlayers")]
        [HarmonyPostfix]
        static void ResetLightsOnLoadPatch(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.FloodlightRotation.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug("Ship reached orbit, attempting to reset floodlights.");

            if (__instance == null)
            {
                ScienceBirdTweaks.Logger.LogError("ResetLightsOnLoadPatch: __instance is null!");
                return;
            }

            ShipFloodlightController controller = __instance.GetComponent<ShipFloodlightController>();

            if (controller != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Found ShipFloodlightController. Calling ResetFloodlightLights().");
                controller.ResetFloodlightLights();
            }
            else
            {
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController component not found on StartOfRound object during ResetLightsOnLoadPatch. Cannot reset lights.");
            }
        }
    }
}