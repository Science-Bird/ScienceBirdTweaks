using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
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
        public static float yOffset = 0f;
        public static Dictionary<string, string> nameShortcuts = new Dictionary<string, string>();
        public static List<TransformAndName> grabbedRadarTargets;
        public static int grabbedIndex = -1;
        private static float camRotX = ScienceBirdTweaks.PlayerCamAngleX.Value;
        private static float camRotY = ScienceBirdTweaks.PlayerCamAngleY.Value;
        private static float camPosF = ScienceBirdTweaks.PlayerCamPosHorizontal.Value;
        private static float camPosY = ScienceBirdTweaks.PlayerCamPosVertical.Value;

        public static Vector3 GetBoxPos(bool original, bool bg)
        {
            return new Vector3(-140f, (original ? -8f : -100f) + yOffset, bg ? 1f : 0f);
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.LateUpdate))]
        [HarmonyPostfix]
        static void CamUpdate(ManualCameraRenderer __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || !(__instance.headMountedCam != null))
            {
                return;
            }
            if (ScienceBirdTweaks.AlterPlayerCam.Value && __instance.enableHeadMountedCam && !__instance.playerIsInCaves)
            {
                StartOfRound.Instance.mapScreenPlayerName.rectTransform.localPosition = GetBoxPos(true, false);
                StartOfRound.Instance.mapScreenPlayerNameBG.rectTransform.localPosition = GetBoxPos(true, true);
                __instance.headMountedCam.transform.Rotate(camRotX, camRotY, 0f, Space.Self);
                __instance.headMountedCam.transform.position += __instance.headMountedCamTarget.up * camPosY + __instance.headMountedCamTarget.forward * camPosF;
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
                    StartOfRound.Instance.mapScreenPlayerName.rectTransform.localPosition = GetBoxPos(false, false);
                    StartOfRound.Instance.mapScreenPlayerNameBG.rectTransform.localPosition = GetBoxPos(false, true);
                }
            }
            else if (ScienceBirdTweaks.HideAllCams.Value && __instance.headMountedCam.enabled)
            {
                __instance.headMountedCam.enabled = false;
                StartOfRound.Instance.mapScreenPlayerName.rectTransform.localPosition = GetBoxPos(false, false);
                StartOfRound.Instance.mapScreenPlayerNameBG.rectTransform.localPosition = GetBoxPos(false, true);
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchRadarTargetForward))]
        [HarmonyPrefix]
        public static void RadarTargets1(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                grabbedRadarTargets = __instance.radarTargets;
                grabbedIndex = (__instance.targetTransformIndex + 1) % __instance.radarTargets.Count;
            }
        }

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchRadarTargetClientRpc))]
        [HarmonyPrefix]
        public static void RadarTargets2(ManualCameraRenderer __instance, int switchToIndex)
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && __instance.radarTargets != null && __instance.radarTargets.Count > 0)
            {
                grabbedRadarTargets = __instance.radarTargets;
                grabbedIndex = switchToIndex;
            }
        }


        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.updateMapTarget), MethodType.Enumerator)]
        [HarmonyPostfix]
        [HarmonyAfter("mborsh.LiveReaction")]
        public static void UpdateMapTargetPatch(ManualCameraRenderer __instance, bool __result, string __state)// this runs twice on server btw
        {
            if (ScienceBirdTweaks.ImprovedTextBox.Value && !__result)// credit to mborsh for this unique kind of patch
            {
                string nameText = "Player";
                if (grabbedRadarTargets != null && grabbedIndex >= 0 && grabbedIndex < grabbedRadarTargets.Count && grabbedRadarTargets[grabbedIndex] != null)
                {
                    nameText = grabbedRadarTargets[grabbedIndex].name;
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Failed to retrieve active player on radar!");
                    return;
                }
                string newName = nameText;
                if (nameShortcuts.TryGetValue(nameText, out string value))
                {
                    nameText = value;
                }
                else
                {
                    if (nameText.Length > 11)
                    {
                        string[] words = nameText.Split(" ");
                        foreach (string word in words)
                        {
                            if (word.Length > 11)
                            {
                                string newWord = System.Text.RegularExpressions.Regex.Replace(word, "[A-Z]", " $0");
                                newName = nameText.Replace(word, newWord);
                            }
                        }
                    }
                    ScienceBirdTweaks.Logger.LogDebug($"Assigning shortcut: {nameText} > {newName}");
                    nameShortcuts.Add(nameText, newName);
                }
                int minIndex = 0;
                if (ScienceBirdTweaks.mborshPresent)
                {
                    nameText = "LIVE " + nameText.ToUpper() + " REACTION";
                    minIndex = 2;
                }
                int index = Mathf.Clamp(Mathf.FloorToInt((float)nameText.Length / 11), minIndex, 4);
                StartOfRound.Instance.mapScreenPlayerName.fontSizeMin = 15f;
                StartOfRound.Instance.mapScreenPlayerName.lineSpacing = -9.6f;
                StartOfRound.Instance.mapScreenPlayerName.text = nameText;
                StartOfRound.Instance.mapScreenPlayerName.rectTransform.sizeDelta = textBoxSizes[index];
                StartOfRound.Instance.mapScreenPlayerNameBG.rectTransform.sizeDelta = textBoxSizes[index];
                yOffset = (textBoxSizes[index].y - 26f) / 2;
            }
        }

    }
}
