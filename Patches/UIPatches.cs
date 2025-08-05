using System.Linq;
using Dissonance;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class UIPatches
    {
        public static Sprite handSprite;
        public static Sprite pointSprite;
        private static readonly string[] vanillaMoons = ["20 Adamance", "68 Artifice", "220 Assurance", "71 Gordion", "7 Dine", "5 Embrion", "41 Experimentation", "44 Liquidation", "61 March", "21 Offense", "85 Rend", "8 Titan", "56 Vow"];

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void InitialLoad()
        {
            if (ScienceBirdTweaks.StretchedHoverIconFix.Value)
            {
                handSprite = (Sprite)ScienceBirdTweaks.TweaksAssets.LoadAsset("HandIcon");
                pointSprite = (Sprite)ScienceBirdTweaks.TweaksAssets.LoadAsset("HandIconPoint");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        [HarmonyPrefix]
        static void FixOnSpawn(PlayerControllerB __instance)
        {
            if ((__instance.IsOwner && __instance.isPlayerControlled && (!__instance.IsServer || __instance.isHostPlayerObject)) || __instance.isTestingPlayer)
            {
                if (__instance.isCameraDisabled)
                {
                    FixHandIcons();
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        [HarmonyPostfix]
        static void FixOnGenerate()
        {
            if (vanillaMoons.Contains(StartOfRound.Instance.currentLevel.PlanetName)) { return; }
            FixHandIcons();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void FixOnLoad(StartOfRound __instance, string sceneName)
        {
            if (sceneName == "SampleSceneRelay" || (sceneName == __instance.currentLevel.sceneName && vanillaMoons.Contains(__instance.currentLevel.PlanetName))) { return; }
            FixHandIcons();
        }

        static void FixHandIcons()
        {
            if (!ScienceBirdTweaks.StretchedHoverIconFix.Value) { return; }
            ScienceBirdTweaks.Logger.LogDebug("Doing hand icon fix!");

            InteractTrigger[] handInteracts = Object.FindObjectsOfType<InteractTrigger>(true).Where(x => x.hoverIcon != null && x.hoverIcon.name == "HandIcon").ToArray();
            InteractTrigger[] pointInteracts = Object.FindObjectsOfType<InteractTrigger>(true).Where(x => x.hoverIcon != null && x.hoverIcon.name == "HandIconPoint").ToArray();

            for (int i = 0; i < handInteracts.Length; i++)
            {
                handInteracts[i].hoverIcon = handSprite;
            }
            for (int i = 0; i < pointInteracts.Length; i++)
            {
                pointInteracts[i].hoverIcon = pointSprite;
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.PlayerMeetsConditionsToBuild))]
        [HarmonyPostfix]
        static void BuildCheckPatch(ShipBuildModeManager __instance, ref bool __result)
        {
            if (ScienceBirdTweaks.FearfulBuilding.Value && __result == false)
            {
                // vanilla checks but without fear level check
                if (__instance.InBuildMode && (__instance.placingObject == null || __instance.placingObject.inUse || StartOfRound.Instance.unlockablesList.unlockables[__instance.placingObject.unlockableID].inStorage))
                {
                    return;
                }
                if (GameNetworkManager.Instance.localPlayerController.isTypingChat)
                {
                    return;
                }
                if (__instance.player.isPlayerDead || __instance.player.inSpecialInteractAnimation || __instance.player.activatingItem)
                {
                    return;
                }
                if (__instance.player.disablingJetpackControls || __instance.player.jetpackControls)
                {
                    return;
                }
                if (!__instance.player.isInHangarShipRoom)
                {
                    return;
                }
                if (StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
                {
                    return;
                }
                if (!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded)
                {
                    return;
                }
                __result = true;
            }
        }

        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Update))]
        [HarmonyPostfix]
        static void PauseUpdate(QuickMenuManager __instance)
        {
            if (ScienceBirdTweaks.PauseMenuFlickerFix.Value && __instance.menuContainer.activeInHierarchy)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    UnityEngine.UI.Button button = EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>();
                    if (button != null && button.animator != null)
                    {
                        button.animator.SetTrigger("Highlighted");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.TryCheckBagContents))]
        [HarmonyPostfix]
        static void BeltBagOpen(BeltBagItem __instance)
        {
            if (ScienceBirdTweaks.CleanBeltBagUI.Value)
            {
                if (ScienceBirdTweaks.test2Present)
                {
                    ModPatches.GoodItemScanPatches.GoodItemScanClearNodes();
                }
                else
                {
                    HUDManager.Instance.scanNodes.Clear();
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.HoldInteractionFill))]
        [HarmonyPrefix]
        static void OnHold(HUDManager __instance)
        {
            if (ScienceBirdTweaks.MissingHoverTipFix.Value && __instance.holdFillAmount == 0f)// at start of a hold interaction
            {
                PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
                if (player.hoveringOverTrigger != null && player.cursorTip.text == "")
                {
                    // set default interact tips first
                    player.cursorIcon.enabled = true;
                    player.cursorIcon.sprite = player.hoveringOverTrigger.hoverIcon;
                    player.cursorTip.text = player.hoveringOverTrigger.hoverTip;
                    if (player.twoHanded && (!player.hoveringOverTrigger.twoHandedItemAllowed || !ScienceBirdTweaks.HandsFullFix.Value))// then check if any special hold tips need to be applied
                    {
                        player.cursorTip.text = "[Hands full]";
                    }
                    else if (!string.IsNullOrEmpty(player.hoveringOverTrigger.holdTip))
                    {
                        player.cursorTip.text = player.hoveringOverTrigger.holdTip;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
        [HarmonyPostfix]
        static void OnHoverTipUpdate(PlayerControllerB __instance)
        {
            if (ScienceBirdTweaks.HandsFullFix.Value && __instance.hoveringOverTrigger != null)
            {
                if (__instance.hoveringOverTrigger.twoHandedItemAllowed && __instance.cursorTip.text == "[Hands full]")
                {
                    if (__instance.hoveringOverTrigger.holdInteraction && HUDManager.Instance.holdFillAmount > 0f)// if in hold interaction, set tip to hold tip if it exists otherwise use hover tip
                    {
                        if (!string.IsNullOrEmpty(__instance.hoveringOverTrigger.holdTip))
                        {
                            __instance.cursorTip.text = __instance.hoveringOverTrigger.holdTip.Replace("[LMB]", "[E]");
                        }
                        else
                        {
                            __instance.cursorTip.text = __instance.hoveringOverTrigger.hoverTip.Replace("[LMB]", "[E]");
                        }
                    }
                    else
                    {
                        __instance.cursorTip.text = __instance.hoveringOverTrigger.hoverTip.Replace("[LMB]", "[E]");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.DisplayTip))]
        [HarmonyPrefix]
        static bool OnWarningTip(HUDManager __instance, bool isWarning)
        {
            if (ScienceBirdTweaks.DisableWarnings.Value && isWarning)
            {
                return false;
            }
            return true;
        }
    }
}
