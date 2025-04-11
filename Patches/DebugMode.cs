using HarmonyLib;
using GameNetcodeStuff;
using Dissonance.Config;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class DebugMode
    {

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNodeIfAffordable))]
        [HarmonyPostfix]
        static void OnTerminalBuy(Terminal __instance, TerminalNode node)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
            if (node.buyItemIndex != -1 && node.buyItemIndex != -7)
            {
                ItemDropship itemDropship = UnityEngine.Object.FindObjectOfType<ItemDropship>();
                if (itemDropship != null && itemDropship.terminalScript != null)
                {
                    ScienceBirdTweaks.Logger.LogInfo($"Buying item, count: {itemDropship.terminalScript.orderedItemsFromTerminal.Count}, ship delivering: {itemDropship.deliveringOrder}");
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogInfo("Null terminal script!");
                }
            }
        }

        [HarmonyPatch(typeof(EclipseWeather), nameof(EclipseWeather.OnEnable))]
        [HarmonyPostfix]
        static void OnEclipsed(EclipseWeather __instance)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
            ScienceBirdTweaks.Logger.LogInfo("Doing eclipsed spawns!");
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.QuitTerminal))]
        [HarmonyPostfix]
        static void OnExitTerminalFix(Terminal __instance)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player != null && player.throwingObject)
            {
                ScienceBirdTweaks.Logger.LogInfo("Fixing player throwing!");
                player.throwingObject = false;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.StopHoldInteractionOnTrigger))]
        [HarmonyPostfix]
        static void OnInteractFail(PlayerControllerB __instance)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.BeginGrabObject))]
        [HarmonyPostfix]
        static void GrabDebug(PlayerControllerB __instance)
        {
            //ScienceBirdTweaks.Logger.LogInfo($"Zap Tutorial: {HUDManager.Instance.setTutorialArrow}");
            //ScienceBirdTweaks.Logger.LogInfo($"Loaded game completions: {ES3.Load("FinishedShockMinigame", "LCGeneralSaveData", 0)}");
        }


        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void FixNoiseSuppression(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
            ScienceBirdTweaks.Logger.LogInfo("Fixing noise suppression...");
            VoiceSettings.Instance.BackgroundSoundRemovalEnabled = false;
        }

    }
}

