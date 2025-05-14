using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using UnityEngine;
using Unity.Netcode;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    internal class ButtonPanelPatches
    {
        private static bool spinnerAdded = false;
        public static GameObject panelPrefab;
        private static bool nullHandler = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeInteractPrefab()
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && (ScienceBirdTweaks.FloodlightRotation.Value || ScienceBirdTweaks.FancyPanel.Value))
            {
                ScienceBirdTweaks.Logger.LogDebug("Initializing button panel!");
                panelPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("ShipFancyPanel");
                NetworkManager.Singleton.AddNetworkPrefab(panelPrefab);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ReplaceButtonPanel(StartOfRound __instance, string sceneName)// load new button interaction
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || (!ScienceBirdTweaks.FloodlightRotation.Value && !ScienceBirdTweaks.FancyPanel.Value) || panelPrefab == null)
            {
                return;
            }

            if (sceneName == "SampleSceneRelay")// if loading into ship
            {
                GameObject buttonPanel = GameObject.Find("Environment/HangarShip/ControlPanelWTexture");
                GameObject ship = GameObject.Find("Environment/HangarShip");
                if (buttonPanel == null || ship == null) { return; }

                if (__instance.IsServer && !GameObject.Find("ShipFancyPanel(Clone)"))
                {
                    ScienceBirdTweaks.Logger.LogDebug("Spawning new button panel...");
                    GameObject newPanel = Object.Instantiate(panelPrefab, buttonPanel.transform.position, buttonPanel.transform.rotation);
                    newPanel.GetComponent<NetworkObject>().Spawn();
                    if (newPanel.GetComponent<NetworkObject>().TrySetParent(ship.transform, worldPositionStays: true))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Parented new panel to ship!");
                    }
                }
                buttonPanel.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartDisconnect))]
        [HarmonyPrefix]
        static void OnDisconnect(GameNetworkManager __instance)// make panel despawning on disconnect less jarring looking by re-enabling base panel
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value && !ScienceBirdTweaks.FloodlightRotation.Value) { return; }
            GameObject buttonPanel = GameObject.Find("Environment/HangarShip/ControlPanelWTexture");
            if (buttonPanel != null)
            {
                buttonPanel.GetComponent<MeshRenderer>().enabled = true;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGameClientRpc))]
        [HarmonyPostfix]
        static void ResetLights()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight1Set(false);
            ButtonPanelController.Instance.GreenLight2Set(false);
            ButtonPanelController.Instance.GreenLight3Set(false);
            ButtonPanelController.Instance.OrangeRoundSet(false);
            ButtonPanelController.Instance.OrangeTallSet(false);
            ButtonPanelController.Instance.RedLightSet(false);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]
        [HarmonyPrefix]
        static void OnResetShip(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight1Set(false);
            ButtonPanelController.Instance.GreenLight2Set(false);
            ButtonPanelController.Instance.GreenLight3Set(false);
            ButtonPanelController.Instance.OrangeRoundSet(false);
            ButtonPanelController.Instance.OrangeTallSet(false);
            ButtonPanelController.Instance.RedLightSet(false);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.FirePlayersAfterDeadlineClientRpc))]
        [HarmonyPostfix]
        static void FiredLights()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.OrangeTallSet(true);
        }

        [HarmonyPatch(typeof(HangarShipDoor), nameof(HangarShipDoor.PlayDoorAnimation))]
        [HarmonyPrefix]
        static void DoorLight(HangarShipDoor __instance, bool closed)
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && ScienceBirdTweaks.FancyPanel.Value && __instance.buttonsEnabled && !StartOfRound.Instance.inShipPhase && ButtonPanelController.Instance != null)
            {
                if (closed && !__instance.shipDoorsAnimator.GetBool("Closed"))
                {
                    ButtonPanelController.Instance.OrangeRoundSet(true);
                }
                else if (!closed && __instance.shipDoorsAnimator.GetBool("Closed"))
                {
                    ButtonPanelController.Instance.OrangeRoundSet(false);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ChangeLevel))]
        [HarmonyPostfix]
        [HarmonyAfter("zigzag.randommoonfx")]
        static void StartRoute(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            if (__instance.travellingToNewLevel)
            {
                ButtonPanelController.Instance.GreenLight1Set(true);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ArriveAtLevel))]
        [HarmonyPostfix]
        static void EndRoute()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight1Set(false);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetPlayersLoadedValueClientRpc))]
        [HarmonyPostfix]
        static void OnStart(bool landingShip)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || !landingShip || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight3Set(true);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPostfix]
        static void OnLand()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight3Set(false);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
        [HarmonyPostfix]
        static void OnTakeoff()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.GreenLight2Set(true);
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ReadDialogue))]
        [HarmonyPostfix]
        static void LeavingDialogue()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            if (TimeOfDay.Instance.shipLeavingAlertCalled)
            {
                ButtonPanelController.Instance.RedLightSet(true);
            }
        }

        [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.PressTeleportButtonClientRpc))]
        [HarmonyPostfix]
        static void OnTeleport()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.BlueLight1Set(true);
            ButtonPanelController.Instance.SetLightAfterDelay(0, 5f, false);
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UseSignalTranslatorClientRpc))]
        [HarmonyPostfix]
        static void OnTransmit()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || !ScienceBirdTweaks.FancyPanel.Value || ButtonPanelController.Instance == null) { return; }
            ButtonPanelController.Instance.BlueLight2Set(true);
            ButtonPanelController.Instance.SetLightAfterDelay(1, 4f, false);
        }

    }
}