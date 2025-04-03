using System.Collections.Generic;
using HarmonyLib;

using SelfSortingStorage.Cupboard;
using static SelfSortingStorage.Cupboard.SmartMemory;
using static System.Text.RegularExpressions.Regex;
using System;
using System.Reflection;
using BepInEx;


namespace ScienceBirdTweaks.ModPatches
{

    public class SSSPatch
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(typeof(Terminal).GetMethod(nameof(Terminal.LoadNewNode)), prefix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("TerminalStoreCheck"), before: ["mrov.terminalformatter"]), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnStoreOpen"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Terminal), nameof(Terminal.Update)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnScrollUpdate"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
            ScienceBirdTweaks.Harmony?.Patch(typeof(Terminal).GetMethod(nameof(Terminal.QuitTerminal)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnExitTerminal"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Terminal), nameof(Terminal.OnEnable)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnTerminalEnable")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(SmartCupboard), nameof(SmartCupboard.StoreObject)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnSmartStore")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(SmartCupboard), "SpawnItem"), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnSmartSpawn")));
        }
    }

    public class SSSTerminalPatch
    {
        static bool storeFlag = false;
        static Dictionary<string, int> smartDict = new Dictionary<string, int>();
        static float previousScrollPos = 1f;

        public static void OnTerminalEnable(Terminal __instance)
        {
            SSSDataRequest dataScript = __instance.gameObject.AddComponent<SSSDataRequest>();
        }

        public static void UpdateDictionary(Terminal terminal)
        {
            MethodInfo method = AccessTools.Method(typeof(SSSDataRequest), nameof(SSSDataRequest.CollectDataServerRpc));
            smartDict = new Dictionary<string, int>();
            SSSDataRequest dataScript = terminal.gameObject.GetComponent<SSSDataRequest>();
            if (dataScript != null)
            {
                method.Invoke(dataScript, new object[] { });
            }
            else
            {
                ScienceBirdTweaks.Logger.LogError("Couldn't find SSS terminal data script!");
            }
        }

        public static void TerminalStoreCheck(Terminal __instance, TerminalNode node)
        {
            if (node.displayText.Contains("[buyableItemsList]") && UnityEngine.Object.FindObjectOfType<SmartCupboard>())
            {
                ScienceBirdTweaks.Logger.LogDebug("Player entering terminal store");
                storeFlag = true;
                UpdateDictionary(__instance);
            }
        }

        public static void OnSmartStore(SmartCupboard __instance)
        {
            Terminal terminalScript = UnityEngine.Object.FindObjectOfType<Terminal>();
            UpdateDictionary(terminalScript);
        }

        public static void OnSmartSpawn(SmartCupboard __instance)
        {
            Terminal terminalScript = UnityEngine.Object.FindObjectOfType<Terminal>();
            UpdateDictionary(terminalScript);
        }

        public static void OnStoreOpen(Terminal __instance)
        {
            if (storeFlag)
            {
                TerminalStoreQuantityReplace(__instance);
            }
        }

        public static void OnScrollUpdate(Terminal __instance)
        {
            if (storeFlag)
            {
                if (MathF.Abs(__instance.scrollBarVertical.value - previousScrollPos) > 0.01f)
                {
                    TerminalStoreQuantityReplace(__instance);
                }
                previousScrollPos = __instance.scrollBarVertical.value;
            }
        }

        public static void TerminalStoreQuantityReplace(Terminal terminal)
        {
            if (storeFlag)
            {
                SSSDataRequest dataScript = terminal.gameObject.GetComponent<SSSDataRequest>();
                if (dataScript != null)
                {
                    smartDict = dataScript.storedDict;
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogError("Couldn't find SSS terminal data script! Failing to update terminal!");
                    return;
                }
                string[] lines = terminal.currentText.Split('\n');
                foreach (string line in lines)
                {
                    string[] spacedLine = line.Split(" ");
                    if (spacedLine.Length > 2)
                    {
                        int spaceIndex = 2;
                        string itemName = spacedLine[1].ToLower();
                        while (!spacedLine[spaceIndex].IsNullOrWhiteSpace())
                        {
                            itemName += " " + spacedLine[spaceIndex].ToLower();
                            spaceIndex++;
                            if (spaceIndex >= spacedLine.Length)
                            {
                                break;
                            }
                        }
                        if (smartDict.TryGetValue(itemName, out int value))
                        {
                            string valueString = Match(line, "(?<!\\$|\\d)\\d+\\s+$").Value;
                            int initialValue = 0;
                            if (!valueString.IsNullOrWhiteSpace())
                            {
                                try
                                {
                                    initialValue = int.Parse(valueString);
                                }
                                catch (FormatException ex)
                                {
                                    ScienceBirdTweaks.Logger.LogError($"Exception on value parse: {ex}");
                                    initialValue = 0;
                                }
                            }
                            if (value > 0)
                            {
                                value--;
                            }
                            lines[Array.IndexOf(lines, line)] = Replace(line, "(?<!\\$|\\d)\\d+\\s+$", (initialValue + value).ToString("D2"));
                        }
                    }
                }
                terminal.currentText = string.Join("\n", lines);
                terminal.screenText.text = terminal.currentText;
            }
        }

        public static void OnExitTerminal(Terminal __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Player exiting terminal store");
            storeFlag = false;
        }
    }
}
