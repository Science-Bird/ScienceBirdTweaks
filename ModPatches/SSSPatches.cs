using System.Collections.Generic;
using HarmonyLib;
using SelfSortingStorage.Cupboard;
using static System.Text.RegularExpressions.Regex;
using System;
using System.Reflection;
using BepInEx;
using ScienceBirdTweaks.Scripts;
using ScienceBirdTweaks.Patches;
using static SelfSortingStorage.Cupboard.SmartMemory;
using System.Drawing;
using UnityEngine;
using System.Linq;
using Steamworks.Ugc;


namespace ScienceBirdTweaks.ModPatches
{

    public class SSSPatches
    {
        public static void DoPatching()
        {
            if (ScienceBirdTweaks.mrovPresent3 && ScienceBirdTweaks.SSSTerminalStock.Value)
            {
                ScienceBirdTweaks.Harmony?.Patch(typeof(Terminal).GetMethod(nameof(Terminal.LoadNewNode)), prefix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("TerminalStoreCheck"), before: ["mrov.terminalformatter"]), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnStoreOpen"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Terminal), nameof(Terminal.Update)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnScrollUpdate"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
                ScienceBirdTweaks.Harmony?.Patch(typeof(Terminal).GetMethod(nameof(Terminal.QuitTerminal)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnExitTerminal"), priority: Priority.Last, after: ["mrov.terminalformatter"]));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(Terminal), nameof(Terminal.OnEnable)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnTerminalEnable")));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(SmartCupboard), nameof(SmartCupboard.StoreObject)), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnSmartStore")));
                ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(SmartCupboard), "SpawnItem"), postfix: new HarmonyMethod(typeof(SSSTerminalPatch).GetMethod("OnSmartSpawn")));
            }
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(AccessTools.TypeByName("SelfSortingStorage.Utils.RoundManagerPatch"), "ResetSmartCupboardIfAllDeads"), prefix: new HarmonyMethod(typeof(SSSKeepScrapPatch).GetMethod("RoundManagerPatchPrefix")));
        }
    }

    public class SSSKeepScrapPatch
    {

        public static bool RoundManagerPatchPrefix()// replace cupboard wiping function with one that checks all the Keep Scrap configs and conditions
        {
            if ((!ScienceBirdTweaks.UsePreventDespawnList.Value && !ScienceBirdTweaks.PreventWorthlessDespawn.Value) || !KeepScrapPatches._isInTargetContext)
            {
                return true;
            }
            AllDeadsReplacement();
            return false;
        }

        public static Item? GetItem(string id)// function imported from SSS Utils
        {
            var idParts = id.Split('/');
            if (idParts == null || idParts.Length <= 1)
                return null;
            if (idParts[0] == "LethalCompanyGame")
                return StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(i => i.itemName.Equals(idParts[1]));
            else
                return SmartMemory.CacheItems.GetValueOrDefault(id);
        }

        public static void AllDeadsReplacement()
        {
            if (RoundManager.Instance == null || !RoundManager.Instance.IsServer || SmartMemory.Instance == null || SmartMemory.Instance.Size == 0)
                return;
            if (StartOfRound.Instance == null || !StartOfRound.Instance.allPlayersDead)
                return;
            int spawnIndex = 0;
            bool spawnFlag = false;
            var cupboard = UnityEngine.Object.FindObjectOfType<SmartCupboard>();
            if (cupboard == null)
                return;

            GameObject cupboardObj = cupboard.gameObject;
            List<(SmartMemory.Data, int)> spawnIndices = new List<(SmartMemory.Data, int)>();

            foreach (var list in SmartMemory.Instance.ItemList)
            {
                foreach (var item in list)
                {
                    Item itemProperties = GetItem(item.Id);
                    if (item.IsValid() && itemProperties != null && itemProperties.isScrap)// anything processed here is a candidate to be removed
                    {
                        if (ScienceBirdTweaks.UsePreventDespawnList.Value && DespawnPrevention.IsStaticallyBlacklisted(item.Id.Split("/")[1]))
                        {
                            if (ScienceBirdTweaks.ZeroDespawnPreventedItems.Value)
                            {
                                for (int i = 0; i < item.Values.Count; i++)
                                {
                                    item.Values[i] = 0;
                                }
                            }
                            ScienceBirdTweaks.Logger.LogDebug($"Found blacklisted item {item.Id} in the smart cupboard, skipping!");
                        }
                        else if (ScienceBirdTweaks.PreventWorthlessDespawn.Value && item.Values.Contains(0))
                        {
                            ScienceBirdTweaks.Logger.LogDebug($"Found zero value items {item.Id} in the smart cupboard!");
                            int itemNum = item.Values.Count;

                            for (int i = 0; i < itemNum; i++)// go through all items, and remove only those with scrap value >0
                            {
                                if (item.Values[i] > 0)
                                {
                                    if (i == 0)
                                    {
                                        spawnFlag = true;
                                        spawnIndices.Add((item,spawnIndex));
                                    }

                                    item.Values.RemoveAt(i);
                                    item.Saves.RemoveAt(i);
                                    item.Quantity--;
                                    itemNum--;
                                    if (item.Quantity <= 0 && SmartMemory.Instance != null)// if we just removed the last item, remove the whole entry
                                    {
                                        item.Id = "INVALID";
                                        SmartMemory.Instance.Size--;
                                        cupboard.placedItems.Remove(spawnIndex);
                                        break;
                                    }
                                }
                            }
                        }
                        else if (SmartMemory.Instance != null)// if no conditions met, remove all items
                        {
                            item.Id = "INVALID";
                            SmartMemory.Instance.Size--;
                            cupboard.placedItems.Remove(spawnIndex);
                        }
                    }
                    spawnIndex++;
                }
            }
            // long story short(ish): all this function does (both in SSS itself and here) is wipe the cupboard's stored values. the actual grabbable items on the shelves are handled entirely by vanilla game logic (and in our case, the Scrap Keeping patches)
            // normally, vanilla will wipe all scrap items anyways, so SSS cupboard data is always synced up
            // the issue here is with keeping zero value items, because the representative grabbable object may or may not be zero value. if it isn't, but some stored values are zero value, the grabbable will be wiped by the stored data will remain, causing a de-sync
            // what this flag and indices do is check if the first item (the representative grabbable) is removed, and if so, we run a spawn call on it, which will call up the next available item in the list to take its place
            if (spawnFlag)
            {
                MethodInfo spawnMethod = AccessTools.Method(typeof(SmartCupboard), "SpawnItem");
                foreach ((SmartMemory.Data,int) data in spawnIndices)
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Spawning {data.Item1.Id} at {data.Item2}");
                    spawnMethod.Invoke(cupboard, new object[] { data.Item1, data.Item2, true });
                }
            }
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
