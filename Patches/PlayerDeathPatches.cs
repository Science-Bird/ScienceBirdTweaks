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

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.PlayGlobalDeathSFX.Value)
            {
                globalDeathSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("GlobalDeathSound");
            }
            if (ScienceBirdTweaks.AutoTeleportBody.Value)
            {
                HUDWarning = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("HUDWarningSFX");
                questionMark = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("QuestionMarkObj");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc))]
        [HarmonyPostfix]
        static void OnPlayerDeath(PlayerControllerB __instance, int playerId)
        {
            if (usedPlayerIDs.Contains(playerId)) { return; }

            usedPlayerIDs.Add(playerId);

            if (ScienceBirdTweaks.AutoTeleportBody.Value && teleportScript == null)
            {
                teleportScript = GameObject.FindObjectOfType<AutoTeleportScript>();
                if (teleportScript == null)
                {
                    GameObject teleportHandler = new GameObject("AutoTeleportScript");
                    teleportScript = teleportHandler.AddComponent<AutoTeleportScript>();
                }
            }

            if (ScienceBirdTweaks.PlayGlobalDeathSFX.Value && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
            {
                //ScienceBirdTweaks.Logger.LogDebug("PLAYING GLOBAL DEATH SFX");
                HUDManager.Instance.UIAudio.PlayOneShot(globalDeathSFX, 0.45f);
                startTime = Time.realtimeSinceStartup;
                if (ScienceBirdTweaks.FancyPanel.Value && ButtonPanelController.Instance != null)
                {
                    ButtonPanelController.Instance.BlueLight2Set(true);
                    ButtonPanelController.Instance.SetLightAfterDelay(1, 1f, false);
                }
            }
            if (ScienceBirdTweaks.AutoTeleportBody.Value && ShipTeleporter.hasBeenSpawnedThisSession)
            {
                teleporter = Object.FindObjectsOfType<ShipTeleporter>().Where(x => !x.isInverseTeleporter).First();
                if (teleporter != null)
                {
                    teleportScript.StartTeleportRoutine(teleporter, playerId);// teleporter stuff offloaded to a monobehaviour
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
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
        [HarmonyPostfix]
        static void OnBeamUp(PlayerControllerB __instance)
        {
            if (ScienceBirdTweaks.UnrecoverableNotification.Value && __instance.shipTeleporterId != -1 && __instance.isPlayerDead)
            {
                if (teleportScript == null)
                {
                    teleportScript = GameObject.FindObjectOfType<AutoTeleportScript>();
                    if (teleportScript == null)
                    {
                        GameObject teleportHandler = new GameObject("AutoTeleportScript");
                        teleportScript = teleportHandler.AddComponent<AutoTeleportScript>();
                    }
                }
                if (teleportScript != null)
                {
                    teleportScript.DisplayCustomScrapBox();
                }
            }
        }
    }
}
