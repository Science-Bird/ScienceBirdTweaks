using HarmonyLib;
using Unity.Netcode;
using System;
using ScienceBirdTweaks.Scripts;
using System.Collections.Generic;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    internal static class KeepScrapPatches
    {
        public static bool _isInTargetContext = false;
        private static bool _isInsideResetShipFurnitureCall = false;
        public static bool extraLogs = false;

        public static void Initialize()
        {
            extraLogs = ScienceBirdTweaks.ExtraLogs.Value;

            string[] itemsToProtect = ScienceBirdTweaks.PreventedDespawnList.Value.Replace(" ", "").Split(",");

            DespawnPrevention.ClearBlacklist();

            foreach (var item in itemsToProtect)
            {
                DespawnPrevention.AddToBlacklist(item);
            }

            string[] itemsSkipText = ScienceBirdTweaks.WorthlessDisplayTextBlacklist.Value.Replace(" ", "").Split(",");

            CustomScanText.ClearBlacklist();

            foreach (var item in itemsSkipText)
            {
                CustomScanText.AddToBlacklist(item);
            }

            ScienceBirdTweaks.Logger.LogInfo("Finished populating scrap keeping blacklists.");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]// set flag on fired to not prevent despawn
        [HarmonyPrefix]
        static void EnterResetShipContextPrefix()
        {
            if (DespawnPrevention.IsBlacklistEmpty() && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return;

            _isInsideResetShipFurnitureCall = true;
            if (extraLogs)
                ScienceBirdTweaks.Logger.LogDebug("Entered ResetShipFurniture Context.");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]// reset flag on fired
        [HarmonyFinalizer]
        static void ExitResetShipContextFinalizer(Exception __exception)
        {
            _isInsideResetShipFurnitureCall = false;
            if (extraLogs)
                ScienceBirdTweaks.Logger.LogDebug("Exited ResetShipFurniture Context.");

            if (__exception != null)
                ScienceBirdTweaks.Logger.LogError($"Exception occurred within ResetShipFurniture: {__exception}");
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]// set flag in despawn method at end of day
        [HarmonyPrefix]
        static void EnterTargetDespawnContextPrefix()
        {
            if ((DespawnPrevention.IsBlacklistEmpty() || !ScienceBirdTweaks.UsePreventDespawnList.Value) && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)// only start doing despawn prevention if we have a usable list or if preventing zero value despawns
                return;

            if (!_isInsideResetShipFurnitureCall)
            {
                _isInTargetContext = true;
                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug("Entered Despawn Prevention Target Context.");
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]// reset end of day flag
        [HarmonyFinalizer]
        static void ExitTargetDespawnContextFinalizer(Exception __exception)
        {
            if ((DespawnPrevention.IsBlacklistEmpty() || !ScienceBirdTweaks.UsePreventDespawnList.Value) && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return;

            if (!_isInsideResetShipFurnitureCall)
            {
                _isInTargetContext = false;
                if (extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug("Exited Despawn Prevention Target Context.");
            }

            if (__exception != null)
                ScienceBirdTweaks.Logger.LogError($"Exception occurred within DespawnPropsAtEndOfRound: {__exception}");
        }

        [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn), new Type[] { typeof(bool) })]
        [HarmonyPrefix]
        static bool CheckDespawnPreventionPrefix(NetworkObject __instance)// when a network object despawns during the end of round phase
        {
            if (!_isInTargetContext)
                return true;
            if (__instance.GetComponent<GrabbableObject>() && (__instance.GetComponent<GrabbableObject>().isInShipRoom || __instance.GetComponent<GrabbableObject>().isInElevator))
                return !DespawnPrevention.ShouldPreventDespawn(__instance);
            
            return true;
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
        [HarmonyPostfix]
        static void UpdateSubtextOnValueSet(GrabbableObject __instance, int setValueTo)// on set scrap value, covers edge case where scrap value is set to zero while inside the ship
        {
            try
            {
                if (setValueTo <= 0)
                {
                    switch (CustomScanText.DoSetSubtext(__instance))
                    {
                        case 0:
                            if (extraLogs)
                                ScienceBirdTweaks.Logger.LogDebug($"DiscardItem Postfix: Conditions not met for item '{__instance.name}', skipping custom text override.");
                            return;
                        case 1:
                            ScienceBirdTweaks.Logger.LogDebug("Setting subtext...");
                            CustomScanText.SetSubtext(__instance, ScienceBirdTweaks.CustomWorthlessDisplayText.Value);
                            break;
                        case 2:
                            ScienceBirdTweaks.Logger.LogDebug("Resetting subtext...");
                            CustomScanText.ResetSubtext(__instance);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception in SetScrapValue Postfix patch: {ex}");
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItemOnClient))]
        [HarmonyPostfix]
        static void UpdateScanNodeSubtextOnValueDiscardLocal(GrabbableObject __instance)// only runs for player dropping
        {
            if (ScienceBirdTweaks.CustomWorthlessDisplayText.Value == "")
                return;
           
            try
            {
                switch (CustomScanText.DoSetSubtext(__instance))// if conditions are met, sync discard functions for other clients
                {
                    case 0:
                        if (extraLogs)
                            ScienceBirdTweaks.Logger.LogDebug($"DiscardItem Postfix: Conditions not met for item '{__instance.name}', skipping custom text override.");
                        return;
                    case 1:
                    case 2:
                        if (!__instance.itemProperties.syncDiscardFunction)
                        {
                            __instance.isSendingItemRPC++;
                            __instance.DiscardItemServerRpc();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception in DiscardItem Postfix patch: {ex}");
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem))]
        [HarmonyPostfix]
        static void UpdateScanNodeSubtextOnValueDiscard(GrabbableObject __instance)// only runs on all clients if the RPCs are called
        {
            if (ScienceBirdTweaks.CustomWorthlessDisplayText.Value == "")
                return;

            try
            {
                switch (CustomScanText.DoSetSubtext(__instance))
                {
                    case 0:
                        if (extraLogs)
                            ScienceBirdTweaks.Logger.LogDebug($"DiscardItem Postfix: Conditions not met for item '{__instance.name}', skipping custom text override.");
                        return;
                    case 1:
                        //ScienceBirdTweaks.Logger.LogDebug("Setting subtext...");
                        CustomScanText.SetSubtext(__instance, ScienceBirdTweaks.CustomWorthlessDisplayText.Value);
                        break;
                    case 2:
                        //ScienceBirdTweaks.Logger.LogDebug("Resetting subtext...");
                        CustomScanText.ResetSubtext(__instance);
                        break;
                }
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception in DiscardItem Postfix patch: {ex}");
            }
        }
    }


    public static class CustomScanText
    {
        private static readonly HashSet<string> _textBlacklist = new HashSet<string>();

        public static void AddToBlacklist(string itemIdentifier)
        {
            if (!string.IsNullOrEmpty(itemIdentifier))
            {
                _textBlacklist.Add(itemIdentifier);
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogInfo($"Added '{itemIdentifier}' to item text blacklist.");
            }
        }

        public static void ClearBlacklist()
        {
            _textBlacklist.Clear();
            if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogInfo("Cleared item text blacklist.");
        }

        public static bool IsStaticallyBlacklisted(string? itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }
            return _textBlacklist.Contains(itemName);
        }

        public static int DoSetSubtext(GrabbableObject targetObject)// set text if an item is in the ship and scrap value is zero, reset text if outside ship (0: reject, 1: set, 2: reset)
        {
            if (ScienceBirdTweaks.CustomWorthlessDisplayText.Value == "")
                return 0;

            if (targetObject.scrapValue > 0 || targetObject.itemProperties == null || !targetObject.itemProperties.isScrap)
            {
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is not a valid zero value scrap item, skipping custom text override.");
                return 0;
            }

            if (!targetObject.isInShipRoom)// outside ship subtext returns to normal
            {
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is not in ship room, resetting custom text.");
                return 2;
            }

            if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is in ship room, checking conditions for custom text override.");

            if (targetObject.isHeld && targetObject.playerHeldBy != null && !targetObject.playerHeldBy.isInHangarShipRoom)
            {
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is held outside ship, skipping custom text override.");
                return 0;
            }

            if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is in ship, checking conditions for custom text override.");

            string? itemName = targetObject.itemProperties?.itemName;

            if (!string.IsNullOrEmpty(itemName) && IsStaticallyBlacklisted(itemName))
            {
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{itemName}' is statically blacklisted, skipping custom text override.");
                return 0;
            }

            return 1;
        }

        public static void SetSubtext(GrabbableObject targetObject, string customText)
        {
            ScanNodeProperties scanNode = targetObject.GetComponentInChildren<ScanNodeProperties>(true);

            if (scanNode != null)
            {
                scanNode.subText = customText;
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogInfo($"SetScrapValue Postfix: Applied custom text '{customText}' to ScanNode for '{targetObject.name}'.");
            }
        }

        public static void ResetSubtext(GrabbableObject targetObject)
        {
            ScanNodeProperties scanNode = targetObject.GetComponentInChildren<ScanNodeProperties>(true);
            if (scanNode != null)
            {
                scanNode.subText = $"Value: ${targetObject.scrapValue}";
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogInfo($"ResetSubtext: Cleared custom text for '{targetObject.name}'.");
            }
        }
    }



    public static class DespawnPrevention
    {
        private static readonly HashSet<string> _despawnBlacklist = new HashSet<string>();

        public static void AddToBlacklist(string itemIdentifier)
        {
            if (!string.IsNullOrEmpty(itemIdentifier))
            {
                _despawnBlacklist.Add(itemIdentifier);
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogInfo($"Added '{itemIdentifier}' to despawn blacklist.");
            }
        }

        public static void RemoveFromBlacklist(string itemIdentifier)
        {
            if (_despawnBlacklist.Remove(itemIdentifier))
            {
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogInfo($"Removed '{itemIdentifier}' from despawn blacklist.");
            }
        }

        public static void ClearBlacklist()
        {
            _despawnBlacklist.Clear();
            if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogInfo("Cleared despawn blacklist.");
        }

        public static bool IsBlacklistEmpty()
        {
            return _despawnBlacklist.Count == 0;
        }

        public static bool IsStaticallyBlacklisted(string? itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }
            return _despawnBlacklist.Contains(itemName);
        }

        public static bool ShouldPreventDespawn(NetworkObject networkObjectInstance)
        {
            if (networkObjectInstance == null)
                return false;

            GrabbableObject grabbable = networkObjectInstance.GetComponent<GrabbableObject>();

            if (grabbable == null)
                return false;

            string? itemName = grabbable.itemProperties?.itemName;
            bool isScrap = grabbable.itemProperties != null && grabbable.itemProperties.isScrap;
            int scrapValue = grabbable.scrapValue;
            bool isHeld = grabbable.isHeld && grabbable.playerHeldBy != null && grabbable.playerHeldBy.isInHangarShipRoom;
            bool isInShip = grabbable.isInShipRoom;
            bool meetsProtectionCriteria = false;
            bool shouldApplyCustomText = false;
            string customText = ScienceBirdTweaks.CustomWorthlessDisplayText.Value;

            if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogDebug($"Checking Despawn: Item='{itemName ?? "N/A"}', Name='{grabbable.name}', Value=${scrapValue}, IsScrap={isScrap}, IsHeld={isHeld}, IsInShip={isInShip}");

            if (ScienceBirdTweaks.UsePreventDespawnList.Value && !string.IsNullOrEmpty(itemName) && _despawnBlacklist.Contains(itemName))
            {
                meetsProtectionCriteria = true;
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName}' is on static blacklist.");
            }
            else if (ScienceBirdTweaks.PreventWorthlessDespawn.Value && isScrap && scrapValue <= 0)
            {
                meetsProtectionCriteria = true;
                if (customText != "")
                {
                    shouldApplyCustomText = true;
                    if (KeepScrapPatches.extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' is zero-value scrap and will have custom text applied.");
                }
                if (KeepScrapPatches.extraLogs)
                    ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' is zero-value scrap.");
            }
            else if (KeepScrapPatches.extraLogs)
                ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' does not meet any protection criteria.");

            if (meetsProtectionCriteria)
            {
                if (isHeld || isInShip)
                {
                    ScienceBirdTweaks.Logger.LogDebug($"Preventing despawn for '{itemName ?? grabbable.name}' because it meets criteria AND is held or in ship.");

                    if (ScienceBirdTweaks.ZeroDespawnPreventedItems.Value && isScrap && scrapValue > 0)
                    {
                        if (KeepScrapPatches.extraLogs)
                            ScienceBirdTweaks.Logger.LogInfo($"Attempting to set scrap value of '{itemName ?? grabbable.name}' to zero (Current: {scrapValue})...");
                        grabbable.SetScrapValue(0);
                    }

                    if (shouldApplyCustomText)
                    {
                        ScanNodeProperties scanNode = grabbable.GetComponentInChildren<ScanNodeProperties>(true);
                        if (scanNode != null)
                        {
                            scanNode.subText = customText;
                            if (KeepScrapPatches.extraLogs)
                                ScienceBirdTweaks.Logger.LogInfo($"Applied custom text for '{itemName ?? grabbable.name}' to '{customText}'.");
                        }
                    }

                    return true;
                }
                else
                {
                    if (KeepScrapPatches.extraLogs)
                        ScienceBirdTweaks.Logger.LogDebug($"Allowing despawn for '{itemName ?? grabbable.name}' because although it meets criteria, it is not held or in ship.");
                    return false;
                }
            }

            return false;
        }
    }
}