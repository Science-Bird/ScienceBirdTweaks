using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class PlayerCamPatches
    {
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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void StartReset(StartOfRound __instance)
        {
            nameText = null;
            nameBG = null;
            twoRadarCam = null;
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
            if (ScienceBirdTweaks.AlterPlayerCam.Value && __instance.enableHeadMountedCam && !__instance.playerIsInCaves)
            {
                SetBoxPos(index, withCam: true);
                __instance.headMountedCam.transform.Rotate(camRotX, camRotY, 0f, Space.Self);
                __instance.headMountedCam.transform.position += __instance.headMountedCamTarget.up * camPosY + __instance.headMountedCamTarget.forward * camPosF;
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
                __instance.headMountedCam.enabled = false;
                SetBoxPos(index, withCam: false);
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchRadarTargetForward))]
        [HarmonyPrefix]
        public static void RadarTargets1(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && __instance.cam == __instance.mapCamera && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                grabbedRadarTargets = __instance.radarTargets;
                grabbedIndices[0] = (__instance.targetTransformIndex + 1) % __instance.radarTargets.Count;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchRadarTargetClientRpc))]
        [HarmonyPrefix]
        public static void RadarTargets2(ManualCameraRenderer __instance, int switchToIndex)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && __instance.cam == __instance.mapCamera && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                grabbedRadarTargets = __instance.radarTargets;
                grabbedIndices[0] = switchToIndex;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.updateMapTarget), MethodType.Enumerator)]
        [HarmonyPostfix]
        [HarmonyAfter("mborsh.LiveReaction")]
        public static void UpdateMapTargetPatch(ManualCameraRenderer __instance, bool __result, string __state)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && !__result && !StartOfRound.Instance.inShipPhase && nameText != null)// credit to mborsh for this unique kind of patch
            {// what camera is this even patching? because it's not any of the ones that actually display the radar map. no idea what mborsh was cooking but it works I guess
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
