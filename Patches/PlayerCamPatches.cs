using System.Collections.Generic;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using GameNetcodeStuff;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;


namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class PlayerCamPatches
    {
        private static bool setupDone = false;
        private static readonly Vector2[] textBoxSizes = [new Vector2(100f, 26f), new Vector2(100f, 36f), new Vector2(100f, 46f), new Vector2(100f, 56f), new Vector2(100f, 66f)];
        public static float[] yOffsets = [0f];
        public static Dictionary<string, string> nameShortcuts = new Dictionary<string, string>();
        public static List<TransformAndName> grabbedRadarTargets;
        public static int[] grabbedIndices = [-1];
        public static TextMeshProUGUI[] nameText;
        public static Image [] nameBG;
        private static float camRotX = ScienceBirdTweaks.PlayerCamAngleX.Value;
        private static float camRotY = ScienceBirdTweaks.PlayerCamAngleY.Value;
        private static float camPosF = ScienceBirdTweaks.PlayerCamPosHorizontal.Value;
        private static float camPosY = ScienceBirdTweaks.PlayerCamPosVertical.Value;
        public static ManualCameraRenderer twoRadarCam;
        private static List<HDAdditionalCameraData> radarCamData = new List<HDAdditionalCameraData>();
        public static bool internalCamDisable = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void StartReset(StartOfRound __instance)
        {
            nameText = null;
            nameBG = null;
            twoRadarCam = null;
            setupDone = false;
            radarCamData.Clear();
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.Start))]
        [HarmonyPostfix]
        static void PlayerCamOverride(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.TrueLocalCam.Value && __instance.cam == __instance.mapCamera && __instance.headMountedCam != null && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                GameObject CamObject = __instance.headMountedCam.gameObject;
                HDAdditionalCameraData cameraData = CamObject.GetComponent<HDAdditionalCameraData>();
                if (cameraData == null) { return; }
                __instance.headMountedCam.cullingMask = 599463771;
                cameraData.customRenderingSettings = true;

                FrameSettingsOverrideMask mask = cameraData.renderingPathCustomFrameSettingsOverrideMask;
                mask.mask[(int)FrameSettingsField.LODBiasMode] = true;
                mask.mask[(int)FrameSettingsField.LODBias] = true;
                mask.mask[(int)FrameSettingsField.MaximumLODLevelMode] = true;
                mask.mask[(int)FrameSettingsField.MaximumLODLevel] = true;
                cameraData.renderingPathCustomFrameSettingsOverrideMask = mask;
                FrameSettings settings = cameraData.renderingPathCustomFrameSettings;
                settings.lodBiasMode = LODBiasMode.OverrideQualitySettings;
                settings.lodBias = 0.5f;
                settings.maximumLODLevelMode = MaximumLODLevelMode.OverrideQualitySettings;
                settings.maximumLODLevel = 1;
                cameraData.renderingPathCustomFrameSettings = settings;
                radarCamData.Add(cameraData);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.UpdateSpecialAnimationValue))]
        [HarmonyPrefix]
        static void OnPlayerSpawn(PlayerControllerB __instance, bool specialAnimation)
        {
            if (ScienceBirdTweaks.TrueLocalCam.Value && !setupDone && !specialAnimation && __instance == StartOfRound.Instance.localPlayerController && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                __instance.thisPlayerModelArms.gameObject.layer = 5;
                LODGroup lodGroup = __instance.meshContainer.gameObject.GetComponentInChildren<LODGroup>();
                LOD[] lods = lodGroup.GetLODs();
                __instance.thisPlayerModelLOD1.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                __instance.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                lods[0].renderers = [__instance.thisPlayerModelLOD1];
                lods[1].renderers = [__instance.thisPlayerModel];
                lodGroup.SetLODs(lods);
                setupDone = true;
            }
        }

        public static void SetCamBias(bool on, int camIndex)
        {
            //ScienceBirdTweaks.Logger.LogDebug($"SET CAM BIAS: {on}");
            for (int i = 0; i < radarCamData.Count; i++)
            {
                if (i != camIndex && camIndex != -100) { continue; }// special value -100 will set all cam biases (e.g. at start of round)
                if (radarCamData[i] != null)
                {
                    FrameSettings settings = radarCamData[i].renderingPathCustomFrameSettings;
                    if (on)
                    {
                        settings.lodBias = 0.5f;
                        settings.maximumLODLevel = 1;
                    }
                    else
                    {
                        settings.lodBias = 1f;
                        settings.maximumLODLevel = 0;
                    }
                    radarCamData[i].renderingPathCustomFrameSettings = settings;
                }
                
            }
        }


        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        static void OnRadarEnable(StartOfRound __instance, bool displayInfo)
        {
            if (ScienceBirdTweaks.zaggyPresent && twoRadarCam == null)
            {
                twoRadarCam = Object.FindObjectOfType<Terminal>().GetComponent<ManualCameraRenderer>();
                yOffsets = [0f, 0f];
                grabbedIndices = [-1, -1];
                if (nameText == null || nameText.Length == 1)
                {
                    GameObject nameObj = GameObject.Find("GameSystems/ItemSystems/TerminalMapScreenUI/MonitoringPlayerUIContainer/PlayerBeingMonitored");
                    if (nameObj != null)
                    {
                        TextMeshProUGUI text = nameObj.GetComponent<TextMeshProUGUI>();
                        if (text != null)
                        {
                            nameText = [StartOfRound.Instance.mapScreenPlayerName, text];
                        }
                    }
                }
                if (nameBG == null || nameBG.Length == 1)
                {
                    GameObject bgObj = GameObject.Find("GameSystems/ItemSystems/TerminalMapScreenUI/MonitoringPlayerUIContainer/PlayerBeingMonitoredBG");
                    if (bgObj != null)
                    {
                        Image bg = bgObj.GetComponent<Image>();
                        if (bg != null)
                        {
                            bg.enabled = true;
                            nameBG = [StartOfRound.Instance.mapScreenPlayerNameBG, bg];
                        }
                    }
                }
            }
            if (nameText == null)
            {
                nameText = [StartOfRound.Instance.mapScreenPlayerName];
            }
            if (nameBG == null)
            {
                nameBG = [StartOfRound.Instance.mapScreenPlayerNameBG];
            }
        }

        public static void SetBoxPos(int index, bool withCam)
        {
            if (nameText.Length != nameBG.Length) { return; }
            //ScienceBirdTweaks.Logger.LogDebug($"SETTING BOX POS, CAM: {withCam}");
            nameText[index].rectTransform.localPosition = new Vector3(-140f, (withCam ? -8f : -100f) + yOffsets[index], 0f);
            nameBG[index].rectTransform.localPosition = new Vector3(-140f, (withCam ? -8f : -100f) + yOffsets[index], 1f);
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.LateUpdate))]
        [HarmonyPostfix]
        static void CamUpdate(ManualCameraRenderer __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || !(__instance.headMountedCam != null) || __instance.cam != __instance.mapCamera || nameText == null || nameText[0] == null)
            {
                return;
            }
            int index = 0;
            if (ScienceBirdTweaks.zaggyPresent && __instance == twoRadarCam)
            {
                index = 1;
                if (nameText.Length <= 1 || nameText[1] == null) { return; }
            }
            if (ScienceBirdTweaks.PlayerCamClipping.Value > 0f && __instance.headMountedCam.farClipPlane != 11f + ScienceBirdTweaks.PlayerCamClipping.Value)
            {
                __instance.headMountedCam.farClipPlane = 11f + ScienceBirdTweaks.PlayerCamClipping.Value;
            }
            if ((ScienceBirdTweaks.HideLocalCam.Value || ScienceBirdTweaks.HideAllCams.Value) && !StartOfRound.Instance.inShipPhase && __instance.targetedPlayer == GameNetworkManager.Instance.localPlayerController && !GameNetworkManager.Instance.localPlayerController.isPlayerDead && !__instance.overrideRadarCameraOnAlways)
            {
                if (__instance.localPlayerPlaceholder != null)
                {
                    __instance.localPlayerPlaceholder.enabled = false;
                }
                if (__instance.headMountedCamUI != null)
                {
                    __instance.headMountedCamUI.enabled = false;
                    SetBoxPos(index, withCam: false);
                }
            }
            else if (ScienceBirdTweaks.HideAllCams.Value && __instance.headMountedCam.enabled)
            {
                if (__instance.headMountedCamUI != null)
                {
                    __instance.headMountedCamUI.enabled = false;
                }
                __instance.headMountedCam.enabled = false;
                SetBoxPos(index, withCam: false);
            }
            if (!internalCamDisable && ScienceBirdTweaks.TrueLocalCam.Value && !ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.HideAllCams.Value && !ScienceBirdTweaks.HideLocalCam.Value && !StartOfRound.Instance.inShipPhase && __instance.targetedPlayer == GameNetworkManager.Instance.localPlayerController && !__instance.targetedPlayer.isPlayerDead && __instance.targetedPlayer.isPlayerControlled && !__instance.overrideRadarCameraOnAlways)
            {
                __instance.enableHeadMountedCam = true;
                __instance.headMountedCam.enabled = true;
                if (__instance.headMountedCamUI != null)
                {
                    __instance.headMountedCamUI.enabled = true;
                }
                if (__instance.localPlayerPlaceholder != null && __instance.localPlayerPlaceholder.enabled)
                {
                    __instance.localPlayerPlaceholder.enabled = false;
                }
                if (__instance.targetedPlayer == null)
                {
                    __instance.headMountedCam.transform.position = __instance.headMountedCamTarget.transform.position + __instance.headMountedCamTarget.up * 1.557f + __instance.headMountedCamTarget.forward * 0.449f;
                    __instance.headMountedCam.transform.rotation = __instance.headMountedCamTarget.transform.rotation;
                    __instance.headMountedCam.transform.Rotate(8.941f, -177.83f, 0f, Space.Self);
                }
                else if (__instance.headMountedCamTarget != null)
                {
                    __instance.headMountedCam.transform.position = __instance.headMountedCamTarget.transform.position + __instance.headMountedCamTarget.up * 0.237f + __instance.headMountedCamTarget.forward * 0.4f;
                    __instance.headMountedCam.transform.rotation = __instance.headMountedCamTarget.transform.rotation;
                    __instance.headMountedCam.transform.Rotate(14.656f, -184.93f, 0f, Space.Self);
                }
            }
            else if (internalCamDisable && ScienceBirdTweaks.TrueLocalCam.Value && !ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.HideAllCams.Value && !ScienceBirdTweaks.HideLocalCam.Value && !__instance.targetedPlayer.isPlayerDead && __instance.targetedPlayer.isPlayerControlled)
            {
                __instance.enableHeadMountedCam = false;
                __instance.headMountedCam.enabled = false;
                __instance.headMountedCamUI.enabled = false;
            }
            if (ScienceBirdTweaks.AlterPlayerCam.Value && __instance.enableHeadMountedCam && !__instance.playerIsInCaves && __instance.headMountedCamTarget != null)
            {
                SetBoxPos(index, withCam: true);
                __instance.headMountedCam.transform.Rotate(camRotX, camRotY, 0f, Space.Self);
                __instance.headMountedCam.transform.position += __instance.headMountedCamTarget.up * camPosY + __instance.headMountedCamTarget.forward * camPosF;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchRadarTargetClientRpc))]
        [HarmonyPrefix]
        public static void RadarTargets2(ManualCameraRenderer __instance, int switchToIndex)// runs for all clients
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && __instance.cam == __instance.mapCamera && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                grabbedRadarTargets = __instance.radarTargets;
                grabbedIndices[0] = switchToIndex;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.updateMapTarget))]
        [HarmonyPrefix]
        public static void UpdateMapTargetPrefix(ManualCameraRenderer __instance, int setRadarTargetIndex, bool calledFromRPC)
        {
            if (ScienceBirdTweaks.TrueLocalCam.Value && !ScienceBirdTweaks.ClientsideMode.Value && __instance.cam == __instance.mapCamera && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"SWITCH {__instance.targetTransformIndex}->{setRadarTargetIndex}; RPC {calledFromRPC}");
                int targetIndex = setRadarTargetIndex;
                if (__instance.radarTargets.Count <= targetIndex)
                {
                    targetIndex = __instance.radarTargets.Count - 1;
                }
                if (!calledFromRPC)
                {
                    for (int i = 0; i < __instance.radarTargets.Count; i++)
                    {
                        if (__instance.radarTargets[targetIndex] == null)
                        {
                            targetIndex = (targetIndex + 1) % __instance.radarTargets.Count;
                            continue;
                        }
                        PlayerControllerB targetPlayer = __instance.radarTargets[targetIndex].transform.gameObject.GetComponent<PlayerControllerB>();
                        if (targetPlayer == null || targetPlayer.isPlayerControlled || targetPlayer.isPlayerDead || targetPlayer.redirectToEnemy != null)
                        {
                            break;
                        }
                        targetIndex = (targetIndex + 1) % __instance.radarTargets.Count;
                    }
                }
                if (__instance.targetTransformIndex != targetIndex)
                {
                    int fromIndex = __instance.targetTransformIndex;
                    //if (__instance.radarTargets[targetIndex] != null && __instance.radarTargets[targetIndex].transform.gameObject.GetComponent<PlayerControllerB>())
                    //{
                    //    ScienceBirdTweaks.Logger.LogDebug($"radar index {fromIndex} -> {targetIndex}");
                    //}
                    //else
                    //{
                    //    ScienceBirdTweaks.Logger.LogDebug($"radar index {fromIndex} -> ({targetIndex})");
                    //}
                    if (fromIndex == targetIndex || __instance.radarTargets[targetIndex] == null) { return; }
                    if (__instance.radarTargets[fromIndex].transform.gameObject.GetComponent<PlayerControllerB>() == GameNetworkManager.Instance.localPlayerController)
                    {
                        //ScienceBirdTweaks.Logger.LogDebug($"CHANGING FROM LOCAL PLAYER TO NON-LOCAL");
                        SetCamBias(false, __instance == twoRadarCam ? 1 : 0);
                    }
                    else
                    {
                        PlayerControllerB newPlayer = __instance.radarTargets[targetIndex].transform.GetComponent<PlayerControllerB>();
                        if (newPlayer != null && newPlayer == GameNetworkManager.Instance.localPlayerController)
                        {
                            //ScienceBirdTweaks.Logger.LogDebug($"CHANGING FROM NON-LOCAL TO LOCAL PLAYER");
                            SetCamBias(true, __instance == twoRadarCam ? 1 : 0);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.updateMapTarget), MethodType.Enumerator)]
        [HarmonyPostfix]
        [HarmonyAfter("mborsh.LiveReaction")]
        public static void UpdateMapTargetPatch(ManualCameraRenderer __instance, bool __result)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && !__result && !StartOfRound.Instance.inShipPhase && nameText != null)// credit to mborsh for this unique kind of patch
            {// but also I still don't really know how this works
                ManualCameraRenderer[] radarMaps = [StartOfRound.Instance.mapScreen];
                if (twoRadarCam != null)
                {
                    radarMaps = [StartOfRound.Instance.mapScreen, twoRadarCam];
                }
                for (int i = 0; i < nameText.Length; i++)
                {
                    if (nameText[i] == null) { continue; }

                    if (i == 1 && grabbedIndices[1] != radarMaps[1].targetTransformIndex)
                    {
                        grabbedIndices[1] = radarMaps[1].targetTransformIndex;
                    }
                    string nameString = "Player";
                    if (grabbedRadarTargets != null && grabbedIndices[i] >= 0 && grabbedIndices[i] < grabbedRadarTargets.Count && grabbedRadarTargets[grabbedIndices[i]] != null)
                    {
                        nameString = grabbedRadarTargets[grabbedIndices[i]].name;
                    }
                    else
                    {
                        return;
                    }
                    string newName = nameString;
                    if (nameShortcuts.TryGetValue(nameString, out string value))
                    {
                        nameString = value;
                    }
                    else
                    {
                        if (nameString.Length > 11)
                        {
                            string[] words = nameString.Split(" ");
                            foreach (string word in words)
                            {
                                if (word.Length > 11)
                                {
                                    string newWord = System.Text.RegularExpressions.Regex.Replace(word, "[A-Z]", " $0");
                                    newName = nameString.Replace(word, newWord);
                                }
                            }
                        }
                        ScienceBirdTweaks.Logger.LogDebug($"Assigning shortcut: {nameString} > {newName}");
                        nameShortcuts.Add(nameString, newName);
                    }
                    int minIndex = 0;
                    if (ScienceBirdTweaks.mborshPresent && (!ScienceBirdTweaks.HideLocalCam.Value || radarMaps[i].targetedPlayer != GameNetworkManager.Instance.localPlayerController))
                    {
                        nameString = "LIVE " + nameString.ToUpper() + " REACTION";
                        minIndex = 2;
                    }
                    int sizeIndex = Mathf.Clamp(Mathf.FloorToInt((float)nameString.Length / 11), minIndex, 4);
                    nameText[i].fontSizeMin = 15f;
                    nameText[i].lineSpacing = -9.6f;
                    nameText[i].text = nameString;
                    if (nameText[i].rectTransform == null || nameBG[i].rectTransform == null) { return; }
                    nameText[i].rectTransform.sizeDelta = textBoxSizes[sizeIndex];
                    nameBG[i].rectTransform.sizeDelta = textBoxSizes[sizeIndex];
                    yOffsets[i] = (textBoxSizes[sizeIndex].y - 26f) / 2;
                }
            }
        }

    }
}
