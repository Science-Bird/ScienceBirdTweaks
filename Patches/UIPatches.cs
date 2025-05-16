using System.Linq;
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

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        [HarmonyPostfix]
        static void OnLevelReady()
        {
            if (!ScienceBirdTweaks.StretchedHoverIconFix.Value || vanillaMoons.Contains(StartOfRound.Instance.currentLevel.PlanetName)) { return; }

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
                    ModPatches.GoodItemScanPatch.GoodItemScanClearNodes();
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
                    if (player.twoHanded)// then check if any special hold tips need to be applied
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
