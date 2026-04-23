using System.Collections.Generic;
using System.Linq;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class MonitorPatches
    {
        public static bool inSetLineContext = false;
        public static Vector3 currentPos = Vector3.zero;
        public static List<Vector3> entrancePositions = new List<Vector3>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        static void DisplayInfoFix(StartOfRound __instance, bool displayInfo)
        {
            if (ScienceBirdTweaks.LocalCam.Value)
            {
                if (ScienceBirdTweaks.TrueLocalCam.Value && !PlayerCamPatches.internalCamDisable && displayInfo)
                {
                    PlayerCamPatches.SetCamBias(false, -100);
                }
                else if (ScienceBirdTweaks.TrueLocalCam.Value && PlayerCamPatches.internalCamDisable && !displayInfo && __instance.mapScreen.targetedPlayer != null && __instance.mapScreen.targetedPlayer == GameNetworkManager.Instance.localPlayerController)
                {
                    PlayerCamPatches.SetCamBias(true, -100);
                }
                PlayerCamPatches.internalCamDisable = displayInfo;
            }
            
            if (ScienceBirdTweaks.MonitorTransitionFix.Value && displayInfo)
            {
                if (__instance.currentLevel.videoReel == null)
                {
                    __instance.screenLevelVideoReel.clip = __instance.currentLevel.videoReel;
                }
                __instance.mapScreen.cam.transform.position = new Vector3(0f, 100f, 0f);
                __instance.mapScreen.mapCamera.nearClipPlane = -0.96f;
                __instance.mapScreen.mapCamera.farClipPlane = 7.52f;
                __instance.radarCanvas.planeDistance = -0.93f;
                FixVideoReelBG(__instance);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        [HarmonyPostfix]
        static void OnConnectionClients(StartOfRound __instance)
        {
            FixVideoReelBG(__instance);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ArriveAtLevel))]
        [HarmonyPostfix]
        static void OnNewLevel(StartOfRound __instance)
        {
            FixVideoReelBG(__instance);
        }

        static void FixVideoReelBG(StartOfRound round)
        {
            if (ScienceBirdTweaks.LocalCam.Value)
            {
                if (!PlayerCamPatches.internalCamDisable && ScienceBirdTweaks.TrueLocalCam.Value)
                {
                    PlayerCamPatches.SetCamBias(false, -100);
                }
                PlayerCamPatches.internalCamDisable = true;
            }
            if (ScienceBirdTweaks.MonitorTransitionFix.Value && round.currentLevel != null && round.currentLevel.videoReel != null)
            {
                float nativeRatio = (float)round.currentLevel.videoReel.width / (float)round.currentLevel.videoReel.height;
                float yModifier = (0.38f / (nativeRatio * 0.2651163f)) * 0.975f;
                GameObject videoParent = round.screenLevelVideoReel.gameObject;
                if (videoParent != null && videoParent.transform.GetChild(0) != null)
                {
                    RectTransform videoBG = videoParent.transform.GetChild(0).GetComponent<RectTransform>();
                    if (videoBG != null)
                    {
                        videoBG.localScale = new Vector3(1f, yModifier, 1f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SetExitIDs))]
        [HarmonyPostfix]
        public static void EntranceSetup(RoundManager __instance)
        {
            if (ScienceBirdTweaks.RadarPathAllExits.Value)
            {
                entrancePositions.Clear();
                entrancePositions = (from x in UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false)
                                     where !x.isEntranceToBuilding
                                     select x.entrancePoint.position).ToList();
                if (RoundManager.Instance.currentDungeonType == 4 && RoundManager.Instance.currentMineshaftElevator != null)
                {
                    Vector3 mainPos = entrancePositions.MaxBy(x => x.y);
                    entrancePositions[entrancePositions.IndexOf(mainPos)] = RoundManager.Instance.currentMineshaftElevator.elevatorBottomPoint.position;
                }
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyPrefix]
        static bool SetLinePrefix(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.RadarPathAllExits.Value && __instance.screenEnabledOnLocalClient)
            {
                inSetLineContext = true;
                currentPos = __instance.mapCamera.transform.position - Vector3.up * 3.75f;
                if (RoundManager.Instance.currentDungeonType == 4 && RoundManager.Instance.currentMineshaftElevator != null)
                {
                    SetLineReplacement(__instance);
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyPostfix]
        static void SetLinePostfix(ManualCameraRenderer __instance)
        {
            inSetLineContext = false;
        }

        static void SetLineReplacement(ManualCameraRenderer cam)
        {
            if (!cam.screenEnabledOnLocalClient || cam.playerIsInCaves)
            {
                cam.lineFromRadarTargetToExit.enabled = false;
                return;
            }
            if (cam.targetedPlayer.isPlayerDead && (cam.targetedPlayer.deadBody == null || !cam.targetedPlayer.deadBody.gameObject.activeSelf || cam.targetedPlayer.deadBody.isInShip || cam.targetedPlayer.deadBody.bodyParts[0].position.y > -50f))
            {
                cam.lineFromRadarTargetToExit.enabled = false;
            }
            if (!cam.targetedPlayer.isInsideFactory)
            {
                cam.lineFromRadarTargetToExit.enabled = false;
                return;
            }
            cam.lineFromRadarTargetToExit.enabled = true;
            if (cam.updateLineInterval > 0f)
            {
                cam.updateLineInterval -= Time.deltaTime;
                cam.dottedLineOffset -= Time.deltaTime;
                cam.lineFromRadarTargetToExit.material.SetTextureOffset("_UnlitColorMap", new Vector2(cam.dottedLineOffset, 0f));
                cam.lineFromRadarTargetToExit.SetPosition(0, cam.mapCamera.transform.position - Vector3.up * 2.5f);
                return;
            }
            Vector3 vector = Vector3.zero;

            Vector3 pos = currentPos;
            float posDiff = 1000000f;
            int index = -1;
            for (int i = 0; i < entrancePositions.Count; i++)
            {
                float diff = (pos - entrancePositions[i]).sqrMagnitude;
                if (diff < posDiff)
                {
                    posDiff = diff;
                    index = i;
                }
            }
            if (index >= 0)
            {
                vector = entrancePositions[index];
            }
            if (vector == Vector3.zero)
            {
                return;
            }
            if (!NavMesh.CalculatePath(cam.mapCamera.transform.position - Vector3.up * 3.75f, vector, -1, cam.path1))
            {
                return;
            }
            if (cam.path1.corners.Length > 50)
            {
                cam.setLineIntervalTo = 2f;
            }
            else if (cam.path1.corners.Length < 36)
            {
                cam.setLineIntervalTo = 0.4f;
            }
            if (cam.path1.corners.Length != 0)
            {
                cam.lineFromRadarTargetToExit.positionCount = Mathf.Min(cam.path1.corners.Length, 20);
                for (int i = 0; i < cam.lineFromRadarTargetToExit.positionCount; i++)
                {
                    cam.path1.corners[i] += Vector3.up * 1.25f;
                }
                cam.lineFromRadarTargetToExit.SetPositions(cam.path1.corners);
            }
            cam.updateLineInterval = cam.setLineIntervalTo;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FindMainEntrancePosition))]
        [HarmonyPostfix]
        static void EntrancePosPatch(RoundManager __instance, ref Vector3 __result)
        {
            if (ScienceBirdTweaks.RadarPathAllExits.Value && inSetLineContext)
            {
                inSetLineContext = false;

                Vector3 pos = currentPos;

                float posDiff = 1000000f;
                int index = -1;
                for (int i = 0; i < entrancePositions.Count; i++)
                {
                    float diff = (pos - entrancePositions[i]).sqrMagnitude;
                    if (diff < posDiff)
                    {
                        posDiff = diff;
                        index = i;
                    }
                }
                if (index >= 0)
                {
                    __result = entrancePositions[index];
                }
            }
        }
    }
}
