using HarmonyLib;
using Unity.Netcode;
using System;
using ScienceBirdTweaks.Scripts;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    internal static class ProtectItemsPatch
    {
        public static void Initialize()
        {
            string[] itemsToProtect = ScienceBirdTweaks.PreventedDespawnList.Value.Replace(" ", "").Split(",");

            ScienceBirdTweaks.Logger.LogInfo("Populating DespawnPrevention blacklist...");
            DespawnPrevention.ClearBlacklist();

            foreach (var item in itemsToProtect)
            {
                DespawnPrevention.AddToBlacklist(item);
            }

            ScienceBirdTweaks.Logger.LogInfo("Finished populating blacklist.");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]
        [HarmonyPrefix]
        static void EnterResetShipContextPrefix()
        {
            if (DespawnPrevention.IsBlacklistEmpty() && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return;

            DespawnPrevention.EnterResetShipFurnitureContext();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]
        [HarmonyFinalizer]
        static void ExitResetShipContextFinalizer(Exception __exception)
        {
            DespawnPrevention.ExitResetShipFurnitureContext();

            if (__exception != null)
                ScienceBirdTweaks.Logger.LogError($"Exception occurred within ResetShipFurniture: {__exception}");
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPrefix]
        static void EnterTargetDespawnContextPrefix()
        {
            if (DespawnPrevention.IsBlacklistEmpty() && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return;

            DespawnPrevention.EnterTargetDespawnContext();
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyFinalizer]
        static void ExitTargetDespawnContextFinalizer(Exception __exception)
        {
             if (DespawnPrevention.IsBlacklistEmpty() && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return;

            DespawnPrevention.ExitTargetDespawnContext();
            if (__exception != null)
                ScienceBirdTweaks.Logger.LogError($"Exception occurred within DespawnPropsAtEndOfRound: {__exception}");
        }

        [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn), new Type[] { typeof(bool) })]
        [HarmonyPrefix]
        static bool CheckDespawnPreventionPrefix(NetworkObject __instance)
        {
            if (__instance.gameObject.GetComponent<GrabbableObject>() && (__instance.gameObject.GetComponent<GrabbableObject>().isInShipRoom || __instance.gameObject.GetComponent<GrabbableObject>().isInElevator))
            if (DespawnPrevention.IsBlacklistEmpty() && !ScienceBirdTweaks.PreventWorthlessDespawn.Value)
                return true;

            return !DespawnPrevention.ShouldPreventDespawn(__instance);
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
        [HarmonyPostfix]
        static void UpdateScannodeSubtextOnValueSet(GrabbableObject __instance, int setValueTo)
        {
            try
            {
                if (setValueTo > 0 || !DoSetSubtext(__instance))
                {
                    ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Conditions not met for item '{__instance.name}', skipping custom text override.");
                    return;
                }

                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Conditions met for item '{__instance.name}'. Applying custom text.");

                SetSubtext(__instance, ScienceBirdTweaks.CustomWorthlessDisplayText.Value);
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception in SetScrapValue Postfix patch: {ex}");
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItemClientRpc))]
        [HarmonyPostfix]
        static void UpdateScannodeSubtextOnValueDiscard(GrabbableObject __instance)
        {
            try
            {
                if (!DoSetSubtext(__instance))
                {
                    ScienceBirdTweaks.Logger.LogDebug($"DiscardItem Postfix: Conditions not met for item '{__instance.name}', skipping custom text override.");
                    return;
                }

                SetSubtext(__instance, ScienceBirdTweaks.CustomWorthlessDisplayText.Value);
            }
            catch (Exception ex)
            {
                ScienceBirdTweaks.Logger.LogError($"Exception in DiscardItem Postfix patch: {ex}");
            }
        }

        private static bool DoSetSubtext(GrabbableObject targetObject)
        {
            if (targetObject.scrapValue > 0)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' has a positive scrap value of {targetObject.scrapValue}, skipping custom text override.");
                return false;
            }

            if (!ScienceBirdTweaks.PreventWorthlessDespawn.Value)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: PreventWorthlessDespawn is disabled, skipping custom text override for item '{targetObject.name}' with value {targetObject.scrapValue}.");
                return false;
            }

            if (targetObject.itemProperties == null || !targetObject.itemProperties.isScrap)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is not scrap, skipping custom text override.");
                return false;
            }

            if (!targetObject.isInShipRoom)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is not in ship room, skipping custom text override.");
                ResetSubtext(targetObject);
                return false;
            }

            ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is in ship room, checking conditions for custom text override.");

            if (targetObject.isHeld && targetObject.playerHeldBy != null && !targetObject.playerHeldBy.isInHangarShipRoom)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is held by player in hangar, skipping custom text override.");
                return false;
            }

            ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{targetObject.name}' is held by player in ship, checking conditions for custom text override.");

            string? itemName = targetObject.itemProperties?.itemName;

            if (!string.IsNullOrEmpty(itemName) && DespawnPrevention.IsStaticallyBlacklisted(itemName))
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Item '{itemName}' is statically blacklisted, skipping custom text override.");
                return false;
            }

            if (ScienceBirdTweaks.CustomWorthlessDisplayText.Value == "")
            {
                ScienceBirdTweaks.Logger.LogDebug($"SetScrapValue Postfix: Custom text is empty for item '{itemName ?? targetObject.name}', leaving default text.");
                return false;
            }

            return true;
        }

        private static void SetSubtext(GrabbableObject targetObject, string customText)
        {
            ScanNodeProperties scanNode = targetObject.GetComponentInChildren<ScanNodeProperties>(true);

            if (scanNode != null)
            {
                scanNode.subText = customText;
                ScienceBirdTweaks.Logger.LogInfo($"SetScrapValue Postfix: Applied custom text '{customText}' to ScanNode for '{targetObject.name}'.");
            }
            else
                ScienceBirdTweaks.Logger.LogError($"SetScrapValue Postfix: Could not find ScanNodeProperties on '{targetObject.name}' to apply custom text!");
        }

        private static void ResetSubtext(GrabbableObject targetObject)
        {
            ScanNodeProperties scanNode = targetObject.GetComponentInChildren<ScanNodeProperties>(true);
            if (scanNode != null)
            {
                scanNode.subText = $"Value: ${targetObject.scrapValue}";
                ScienceBirdTweaks.Logger.LogInfo($"ResetSubtext: Cleared custom text for '{targetObject.name}'.");
            }
            else
                ScienceBirdTweaks.Logger.LogError($"ResetSubtext: Could not find ScanNodeProperties on '{targetObject.name}' to clear custom text!");
        }
    }
}