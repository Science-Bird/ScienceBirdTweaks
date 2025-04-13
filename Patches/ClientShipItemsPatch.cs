using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ClientShipItemsPatch
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPrefix]
        static void ClientConnectSync(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ClientShipItems.Value && __instance.inShipPhase)
            {
                GrabbableObject[] grabbables = Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (GrabbableObject grabbable in grabbables)
                {
                    grabbable.fallTime = 1f;
                    grabbable.hasHitGround = true;
                    grabbable.scrapPersistedThroughRounds = true;
                    grabbable.isInElevator = true;
                    grabbable.isInShipRoom = true;
                }
            }
        }
    }
}
