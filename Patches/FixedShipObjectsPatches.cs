using UnityEngine;
using HarmonyLib;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class FixedShipObjectsPatches
    {

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.PositionSuitsOnRack))]
        [HarmonyPostfix]
        static void ParentSuits(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.FixedSuitRack.Value)
            {
                return;
            }
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            if (hangarShip != null)
            {
                UnlockableSuit[] suits = UnityEngine.Object.FindObjectsOfType<UnlockableSuit>();
                ScienceBirdTweaks.Logger.LogDebug("Fixing suits...");
                foreach (UnlockableSuit suit in suits)
                {
                    GameObject suitObj = suit.gameObject;
                    suitObj.transform.SetParent(hangarShip.transform, worldPositionStays: true);
                }
            }
        }

        static void ParentButton()
        {
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            GameObject teleportButton = GameObject.Find("/Teleporter(Clone)/ButtonContainer");
            GameObject inverseTeleportButton = GameObject.Find("/InverseTeleporter(Clone)/ButtonContainer");
            if (hangarShip != null && teleportButton != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Fixing teleporter button...");
                teleportButton.transform.SetParent(hangarShip.transform, worldPositionStays: true);
            }
            if (hangarShip != null && inverseTeleportButton != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Fixing inverse teleporter button...");
                inverseTeleportButton.transform.SetParent(hangarShip.transform, worldPositionStays: true);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableClientRpc))]
        [HarmonyPostfix]
        static void ParentButtonOnBuy(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.FixedTeleporterButton.Value)
            {
                ParentButton();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ParentButtonOnLoad(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.FixedTeleporterButton.Value)
            {
                ParentButton();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        [HarmonyPostfix]
        static void ParentButtonOnSpawn(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.FixedTeleporterButton.Value)
            {
                ParentButton();
            }
        }
    }
}

