using System.Collections.Generic;
using HarmonyLib;
using SelfSortingStorage.Cupboard;
using static System.Text.RegularExpressions.Regex;
using System;
using System.Reflection;
using BepInEx;
using ScienceBirdTweaks.Scripts;


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
                method.Invoke(dataScript, new object[] { });// run data request script to give all clients latest items
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

        public static void OnSmartStore(SmartCupboard __instance)// when an item is stored
        {
            Terminal terminalScript = UnityEngine.Object.FindObjectOfType<Terminal>();
            UpdateDictionary(terminalScript);
        }

        public static void OnSmartSpawn(SmartCupboard __instance)// when the cupboard spawns an item
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
                if (MathF.Abs(__instance.scrollBarVertical.value - previousScrollPos) > 0.01f)// update terminal text as the player scrolls
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
                    if (spacedLine.Length > 2)// should always be true for any normal valid terminal line
                    {
                        int spaceIndex = 2;
                        string itemName = spacedLine[1].ToLower();
                        while (!spacedLine[spaceIndex].IsNullOrWhiteSpace())// to catch items with multi-word names, keep checking for additional words until none can be found
                        {
                            itemName += " " + spacedLine[spaceIndex].ToLower();
                            spaceIndex++;
                            if (spaceIndex >= spacedLine.Length)
                            {
                                break;
                            }
                        }
                        if (smartDict.TryGetValue(itemName, out int value))// try to match item name found in terminal line with the dictionary
                        {
                            string valueString = Match(line, "(?<!\\$|\\d)\\d+\\s+$").Value;// grabs a number at the end of the line which is not preceded by a $ (meaning it isn't the item's price)
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
                            if (value > 0)// if an item is stored in the cupboard, it will always display as "1 owned" on the terminal, this resets it back to zero for calculations
                            {
                                value--;
                            }
                            lines[Array.IndexOf(lines, line)] = Replace(line, "(?<!\\$|\\d)\\d+\\s+$", (initialValue + value).ToString("D2"));// same string check as before but now actually replaces the value with a new one
                        }
                    }
                }
                terminal.currentText = string.Join("\n", lines);// update terminal's actual text
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
