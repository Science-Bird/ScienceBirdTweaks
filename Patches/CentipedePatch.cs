using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class CentipedePatch
    {
        public static bool multiplayerSecondChanceGiven = false;

        private static int maxHealth = 100;

        private static int damageAccumulated = 0;

        private static bool subtractInterval = false;

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        static void GetMaxHealth(RoundManager __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            damageAccumulated = 0;
            maxHealth = GameNetworkManager.Instance.localPlayerController.health;
            ScienceBirdTweaks.Logger.LogDebug($"Found max health: {maxHealth}");// find max health at level start (in case it isn't 100 for whatever reason)
        }

        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.Update))]
        [HarmonyPrefix]
        static void CentipedeClearDamage(CentipedeAI __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            if (ScienceBirdTweaks.CentipedeMode.Value == "Fixed Damage")
            {
                if (__instance.clingingToPlayer == null)
                {
                    return;
                }
                if (__instance.clingingToPlayer.isPlayerDead)// clear accumulated damage when a player dies
                {
                    subtractInterval = false;
                    damageAccumulated = 0;
                }
            }
        }

        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.DamagePlayerOnIntervals))]
        [HarmonyPrefix]
        static void CentipedeDamage(CentipedeAI __instance)
        {
            if (ScienceBirdTweaks.CentipedeMode.Value == "Vanilla")
            {
                return;
            }
            if (subtractInterval)// vanilla method for doing damage on a certain interval, this patch essentially replaces vanilla logic
            {
                __instance.damagePlayerInterval -= Time.deltaTime;
            }
            subtractInterval = false;
            if (__instance.damagePlayerInterval <= 0f && !__instance.inDroppingOffPlayerAnim)
            {
                if (__instance.stunNormalizedTimer > 0f || (((ScienceBirdTweaks.CentipedeMode.Value == "Second Chance" && !multiplayerSecondChanceGiven) || (StartOfRound.Instance.connectedPlayersAmount <= 0 && !__instance.singlePlayerSecondChanceGiven && ScienceBirdTweaks.CentipedeMode.Value != "Fixed Damage")) && __instance.clingingToPlayer.health <= ScienceBirdTweaks.CentipedeSecondChanceThreshold.Value))
                {// drop off player, this covers both vanilla second chance behaviour, and the second chance behaviour added by this mod. essentially this runs if: solo (and second chance not given yet), multiplayer with second chance mode (and second chance not given yet), and only after a player is reduced to a certain HP threshold
                    ScienceBirdTweaks.Logger.LogDebug($"Giving second chance!");
                    if (StartOfRound.Instance.connectedPlayersAmount <= 0)
                    {
                        __instance.singlePlayerSecondChanceGiven = true;
                    }
                    else
                    {
                        multiplayerSecondChanceGiven = true;
                    }
                    __instance.inDroppingOffPlayerAnim = true;
                    __instance.StopClingingServerRpc(playerDead: false);
                }
                else if (damageAccumulated < Mathf.RoundToInt(maxHealth * ScienceBirdTweaks.CentipedeFixedDamage.Value) || ScienceBirdTweaks.CentipedeMode.Value != "Fixed Damage" || __instance.clingingToPlayer.criticallyInjured)
                {// main damage loop, runs if in fixed mode and threshold not met yet, if not in fixed mode, or if the player is critically injured
                    if (ScienceBirdTweaks.CentipedeMode.Value == "Fixed Damage")
                    {
                        damageAccumulated += 10;
                        ScienceBirdTweaks.Logger.LogDebug($"Accumulated damage: {damageAccumulated}");
                    }
                    __instance.clingingToPlayer.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Suffocation);
                    __instance.damagePlayerInterval = 2f;
                }
                else// otherwise, drop off and clear accumulated damage
                {
                    ScienceBirdTweaks.Logger.LogDebug("Dropping off player");
                    __instance.inDroppingOffPlayerAnim = true;
                    __instance.StopClingingServerRpc(playerDead: false);
                    damageAccumulated = 0;
                }
            }
            else
            {
                subtractInterval = true;
            }
            if (__instance.damagePlayerInterval <= 0f && !__instance.inDroppingOffPlayerAnim)
            {
                __instance.damagePlayerInterval = 0.01f;// ensure actual game code never runs
            }
        }
    }
}
