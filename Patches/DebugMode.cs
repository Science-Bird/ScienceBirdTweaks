using System;
using System.Reflection;
using System.Text;
using Dissonance.Config;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class DebugMode
    {
        private static string GetObjectPath(GameObject obj)
        {
            StringBuilder path = new StringBuilder(obj.name);
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }

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
                    ScienceBirdTweaks.Logger.LogWarning("Null terminal script!");
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

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
        [HarmonyPostfix]
        static void SuitTooltip(PlayerControllerB __instance)
        {
            if (ScienceBirdTweaks.DebugMode.Value && __instance.cursorTip.text != "" && __instance.cursorTip.text.Contains("_Suit"))
            {
                __instance.cursorTip.text = __instance.cursorTip.text.Replace("_Suit", " suit");
                __instance.cursorTip.text = __instance.cursorTip.text.Replace("WineRed", " Wine red");
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

        //[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.BeginGrabObject))]
        //[HarmonyPostfix]
        //static void GrabDebug(PlayerControllerB __instance)
        //{
        //    if (__instance.isCrouching)
        //    {

        //    }
        //}

        public static void ObjectDebugger(object obj, string tag)
        {
            if (obj != null)
            {
                Type type = obj.GetType();

                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (PropertyInfo property in properties)
                {
                    object value = property.GetValue(obj, null);
                    ScienceBirdTweaks.Logger.LogDebug($"({tag}) PROPERTY: {property.Name} - {value}");
                }

                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(obj);
                    ScienceBirdTweaks.Logger.LogDebug($"({tag}) FIELD: {field.Name} - {value}");
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void FixNoiseSuppression(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DebugMode.Value)
            {
                return;
            }
            VoiceSettings.Instance.BackgroundSoundRemovalEnabled = false;
        }
    }
}

