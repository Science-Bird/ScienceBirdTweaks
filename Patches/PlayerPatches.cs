using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class PlayerPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPostfix]
        static void OnDamaged(PlayerControllerB __instance)// disable damage animation
        {
            if (ScienceBirdTweaks.CrouchDamageAnimation.Value && __instance.playerBodyAnimator != null && (__instance.playerBodyAnimator.GetBool("crouching") || __instance.isCrouching))
            {
                __instance.playerBodyAnimator.ResetTrigger("Damage");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetObjectAsNoLongerHeld))]
        [HarmonyPostfix]
        static void OnDrop(PlayerControllerB __instance)// check carry inventory on item dropped
        {
            if (ScienceBirdTweaks.ZeroWeightCheck.Value && __instance.ItemSlots.All(x => x == null))
            {
                if (__instance.carryWeight != 1f)
                {
                    ScienceBirdTweaks.Logger.LogDebug($"{__instance.carryWeight == 1f}. No items in inventory! Resetting weight.");
                    __instance.carryWeight = 1f;
                }
            }
        }
    }
}
