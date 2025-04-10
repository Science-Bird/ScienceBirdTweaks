using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public static class DespawnPrevention
    {
        private static bool _isInTargetContext = false;
        private static bool _isInsideResetShipFurnitureCall = false;
        private static readonly HashSet<string> _despawnBlacklist = new HashSet<string>();

        public static void EnterResetShipFurnitureContext()
        {
            _isInsideResetShipFurnitureCall = true;
            ScienceBirdTweaks.Logger.LogDebug("Entered ResetShipFurniture Context.");
        }

        public static void ExitResetShipFurnitureContext()
        {
            _isInsideResetShipFurnitureCall = false;
            ScienceBirdTweaks.Logger.LogDebug("Exited ResetShipFurniture Context.");
        }

        public static void EnterTargetDespawnContext()
        {
            if (!_isInsideResetShipFurnitureCall)
            {
                _isInTargetContext = true;
                ScienceBirdTweaks.Logger.LogDebug("Entered Despawn Prevention Target Context.");
            }
        }

        public static void ExitTargetDespawnContext()
        {
            if (!_isInsideResetShipFurnitureCall)
            {
                _isInTargetContext = false;
                ScienceBirdTweaks.Logger.LogDebug("Exited Despawn Prevention Target Context.");
            }
        }

        public static bool IsInTargetContext()
        {
            return _isInTargetContext;
        }

        public static void AddToBlacklist(string itemIdentifier)
        {
            if (!string.IsNullOrEmpty(itemIdentifier))
            {
                _despawnBlacklist.Add(itemIdentifier);
                ScienceBirdTweaks.Logger.LogInfo($"Added '{itemIdentifier}' to despawn blacklist.");
            }
        }

        public static void RemoveFromBlacklist(string itemIdentifier)
        {
            if (_despawnBlacklist.Remove(itemIdentifier))
            {
                ScienceBirdTweaks.Logger.LogInfo($"Removed '{itemIdentifier}' from despawn blacklist.");
            }
        }

        public static void ClearBlacklist()
        {
            _despawnBlacklist.Clear();
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
            if (!_isInTargetContext)
                return false;

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

            ScienceBirdTweaks.Logger.LogDebug($"Checking Despawn: Item='{itemName ?? "N/A"}', Name='{grabbable.name}', Value=${scrapValue}, IsScrap={isScrap}, IsHeld={isHeld}, IsInShip={isInShip}, Context={_isInTargetContext}");

            if (!string.IsNullOrEmpty(itemName) && _despawnBlacklist.Contains(itemName))
            {
                meetsProtectionCriteria = true;
                ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName}' is on static blacklist.");
            }
            else if (ScienceBirdTweaks.PreventWorthlessDespawn.Value && isScrap && scrapValue <= 0)
            {
                meetsProtectionCriteria = true;
                if (customText != "")
                {
                    shouldApplyCustomText = true;
                    ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' is zero-value scrap and will have custom text applied.");
                }
                ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' is zero-value scrap.");
            }
            else
                ScienceBirdTweaks.Logger.LogDebug($"Item '{itemName ?? grabbable.name}' does not meet any protection criteria.");

            if (meetsProtectionCriteria)
            {
                if (isHeld || isInShip)
                {
                    ScienceBirdTweaks.Logger.LogInfo($"Preventing despawn for '{itemName ?? grabbable.name}' because it meets criteria AND is held or in ship.");

                    if (ScienceBirdTweaks.ZeroDespawnPreventedItems.Value && isScrap && scrapValue > 0)
                    {
                        ScienceBirdTweaks.Logger.LogInfo($"Attempting to set scrap value of '{itemName ?? grabbable.name}' to zero (Current: {scrapValue})...");
                        grabbable.SetScrapValue(0);
                    }

                    if (shouldApplyCustomText)
                    {
                        ScanNodeProperties scanNode = grabbable.GetComponentInChildren<ScanNodeProperties>(true);
                        if (scanNode != null)
                        {
                            scanNode.subText = customText;
                            ScienceBirdTweaks.Logger.LogInfo($"Applied custom text for '{itemName ?? grabbable.name}' to '{customText}'.");
                        }
                        else
                            ScienceBirdTweaks.Logger.LogError($"Failed to apply custom text for '{itemName ?? grabbable.name}' ScanNodeProperties is null.");
                    }

                    return true;
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogDebug($"Allowing despawn for '{itemName ?? grabbable.name}' because although it meets criteria, it is not held or in ship.");
                    return false;
                }
            }

            return false;
        }
    }
}