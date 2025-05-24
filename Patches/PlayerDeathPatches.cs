using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using System.Collections;
using ScienceBirdTweaks.Scripts;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Authentication.Internal;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class PlayerDeathPatches
    {
        public static AudioClip globalDeathSFX;
        public static AudioClip HUDWarning;
        public static GameObject questionMark;
        public static ShipTeleporter teleporter;
        public static AutoTeleportScript teleportScript;
        public static List<int> usedPlayerIDs = new List<int>();
        public static float startTime = 0f;
        public static GameObject teleportScriptPrefab;


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.PlayGlobalDeathSFX.Value)
            {
                globalDeathSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("GlobalDeathSound");
            }
            if (!ScienceBirdTweaks.ClientsideMode.Value && (ScienceBirdTweaks.UnrecoverableNotification.Value || ScienceBirdTweaks.AutoTeleportBody.Value))
            {
                HUDWarning = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("HUDWarningSFX");
                questionMark = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("QuestionMarkObj");
                teleportScriptPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("AutoTeleportScript");
                NetworkManager.Singleton.AddNetworkPrefab(teleportScriptPrefab);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void SpawnTeleportScript(StartOfRound __instance, string sceneName)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || (!ScienceBirdTweaks.AutoTeleportBody.Value && !ScienceBirdTweaks.UnrecoverableNotification.Value)) { return; }
            if (teleportScript == null && __instance.IsServer)
            {
                GameObject teleportScriptObj = UnityEngine.Object.Instantiate(teleportScriptPrefab, Vector3.zero, Quaternion.identity);
                teleportScriptObj.GetComponent<NetworkObject>().Spawn();
                teleportScript = teleportScriptObj.GetComponent<AutoTeleportScript>();
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc))]
        [HarmonyPostfix]
        static void OnPlayerDeath(PlayerControllerB __instance, int playerId)
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || (!ScienceBirdTweaks.AutoTeleportBody.Value && !ScienceBirdTweaks.PlayGlobalDeathSFX.Value && !ScienceBirdTweaks.UnrecoverableNotification.Value) || usedPlayerIDs.Contains(playerId)) { return; }

            usedPlayerIDs.Add(playerId);
            startTime = Time.realtimeSinceStartup;

            if (ScienceBirdTweaks.AutoTeleportBody.Value && teleportScript == null)
            {
                teleportScript = GameObject.FindObjectOfType<AutoTeleportScript>();
            }

            if (ScienceBirdTweaks.PlayGlobalDeathSFX.Value && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
            {
                //ScienceBirdTweaks.Logger.LogDebug("PLAYING GLOBAL DEATH SFX");
                HUDManager.Instance.UIAudio.PlayOneShot(globalDeathSFX, 0.45f);
                if (ScienceBirdTweaks.FancyPanel.Value && ButtonPanelController.Instance != null)
                {
                    ButtonPanelController.Instance.BlueLight2Set(true);
                    ButtonPanelController.Instance.SetLightAfterDelay(1, 1f, false);
                }
            }
            if (ScienceBirdTweaks.AutoTeleportBody.Value && ShipTeleporter.hasBeenSpawnedThisSession)
            {
                if (teleporter == null)
                {
                    ShipTeleporter[] teleporters = Object.FindObjectsOfType<ShipTeleporter>().Where(x => !x.isInverseTeleporter).ToArray();
                    if (teleporters.Length > 0)
                    {
                        teleporter = teleporters.First();
                    }
                }
                if (teleporter != null)
                {
                    teleportScript.StartTeleportRoutine(teleporter, playerId);// teleporter stuff offloaded to a network behaviour
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.Update))]
        [HarmonyPostfix]
        static void ClearListAfterTime(RoundManager __instance)
        {
            if (startTime != 0f && Time.realtimeSinceStartup - startTime > 5f)
            {
                startTime = 0f;
                usedPlayerIDs.Clear();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPostfix]
        static void ClearListOnRevive(StartOfRound __instance)
        {
            usedPlayerIDs.Clear();
            if (teleportScript != null)
            {
                teleportScript.playerQueue.Clear();
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
        [HarmonyPostfix]
        static void OnBeamUp(PlayerControllerB __instance)
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && ScienceBirdTweaks.UnrecoverableNotification.Value && ShipTeleporter.hasBeenSpawnedThisSession && __instance.isPlayerDead && !StartOfRound.Instance.inShipPhase && __instance.shipTeleporterId != -1)
            {
                if (teleporter == null)
                {
                    ShipTeleporter[] teleporters = Object.FindObjectsOfType<ShipTeleporter>().Where(x => !x.isInverseTeleporter).ToArray();
                    if (teleporters.Length > 0)
                    {
                        teleporter = teleporters.First();
                    }
                }
                if (teleporter != null && System.Array.Exists(teleporter.playersBeingTeleported, x => x == (int)__instance.playerClientId))
                {
                    if (teleportScript == null)
                    {
                        teleportScript = GameObject.FindObjectOfType<AutoTeleportScript>();
                    }
                    if (teleportScript != null)
                    {
                        teleportScript.DisplayBoxAfterCheck((int)__instance.playerClientId);
                    }
                }
            }
        }
    }
}
