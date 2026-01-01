using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class MonitorPatches
    {
        public static ManualCameraRenderer twoRadarCam;
        public static bool inSetLineContext = false;
        public static Vector3 currentPos = Vector3.zero;
        public static List<Vector3> entrancePositions = new List<Vector3>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void StartReset(StartOfRound __instance)
        {
            twoRadarCam = null;
            
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        static void DisplayInfoFix(StartOfRound __instance, bool displayInfo)
        {
            if (ScienceBirdTweaks.TrueLocalCam.Value)
            {
                if (!PlayerCamPatches.internalCamDisable && displayInfo)
                {
                    PlayerCamPatches.SetCamBias(false, -100);
                }
                else if (PlayerCamPatches.internalCamDisable && !displayInfo && __instance.mapScreen.targetedPlayer != null && __instance.mapScreen.targetedPlayer == GameNetworkManager.Instance.localPlayerController)
                {
                    PlayerCamPatches.SetCamBias(true, -100);
                }
                    PlayerCamPatches.internalCamDisable = displayInfo;
            }
            
            if (ScienceBirdTweaks.MonitorTransitionFix.Value && displayInfo)
            {
                if (__instance.currentLevel.videoReel == null)
                {
                    __instance.screenLevelVideoReel.enabled = false;
                    __instance.screenLevelVideoReel.gameObject.SetActive(false);
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
            if (ScienceBirdTweaks.TrueLocalCam.Value)
            {
                if (!PlayerCamPatches.internalCamDisable)
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
        static void SetLinePrefix(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.RadarPathAllExits.Value && !ScienceBirdTweaks.butteryPresent && __instance.screenEnabledOnLocalClient)
            {
                inSetLineContext = true;
                currentPos = __instance.mapCamera.transform.position - Vector3.up * 3.75f;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyPostfix]
        static void SetLinePostfix(ManualCameraRenderer __instance)
        {
            inSetLineContext = false;
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
