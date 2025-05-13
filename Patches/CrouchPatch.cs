using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class CrouchPatch
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
    }
}
