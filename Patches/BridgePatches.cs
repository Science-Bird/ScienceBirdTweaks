using System.ComponentModel;
using System.Threading;
using HarmonyLib;
using JLLItemsModule.Components;
using Unity;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class BridgePatches
    {
        private static BridgeTriggerType2 bridge2;
        private static bool bridge1Present = false;
        private static bool bridge2Present = false;
        private static bool doneBridge1 = false;
        private static bool doneBridge2 = false;
        private static float startTime1 = 0f;
        private static float startTime2 = 0f;
        private static Animator bridge2Animator;
        private static int bridge2Count = 0;

        [HarmonyPatch(typeof(BridgeTrigger), nameof(BridgeTrigger.BridgeFallClientRpc))]
        [HarmonyPostfix]
        static void BridgeFall(BridgeTrigger __instance)
        {
            if (ScienceBirdTweaks.BridgeItemsFix.Value)
            {
                startTime1 = Time.realtimeSinceStartup;
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
        [HarmonyPostfix]
        static void BridgeCheckReset(BridgeTrigger __instance)// reset static values and check if level has a bridge
        {
            if (!ScienceBirdTweaks.BridgeItemsFix.Value) { return; }

            doneBridge1 = false;
            doneBridge2 = false;
            startTime1 = 0f;
            startTime2 = 0f;
            bridge2Count = 0;
            bridge2 = Object.FindObjectOfType<BridgeTriggerType2>();
            if (bridge2 != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Found bridge 2!");
                bridge2Present = true;
                bridge2Animator = bridge2.animatedObjectTrigger.triggerAnimator;
            }
            if (Object.FindObjectOfType<BridgeTrigger>())
            {
                ScienceBirdTweaks.Logger.LogDebug("Found bridge 1!");
                bridge1Present = true;
            }
        }

        [HarmonyPatch(typeof(AnimatedObjectTrigger), nameof(AnimatedObjectTrigger.TriggerAnimation))]
        [HarmonyPostfix]
        static void Bridge2AnimateServer(AnimatedObjectTrigger __instance)
        {
            if (__instance.IsServer && ScienceBirdTweaks.BridgeItemsFix.Value && bridge2Present && __instance.triggerAnimator == bridge2Animator)
            {
                bridge2Count++;
                if (bridge2Count == 2)
                {
                    startTime2 = Time.realtimeSinceStartup;
                }
            }
        }

        [HarmonyPatch(typeof(AnimatedObjectTrigger), nameof(AnimatedObjectTrigger.UpdateAnimTriggerClientRpc))]
        [HarmonyPostfix]
        static void Bridge2AnimateClient(AnimatedObjectTrigger __instance)
        {

            if (!__instance.IsServer && ScienceBirdTweaks.BridgeItemsFix.Value && bridge2Present && __instance.triggerAnimator == bridge2Animator)
            {
                bridge2Count++;
                if (bridge2Count == 2)
                {
                    startTime2 = Time.realtimeSinceStartup;
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.Update))]
        [HarmonyPostfix]
        static void BridgeUpdates(RoundManager __instance)
        {
            if (!ScienceBirdTweaks.BridgeItemsFix.Value || (!bridge1Present && !bridge2Present)) { return; }

            if (startTime1 != 0f && Time.realtimeSinceStartup - startTime1 > 1.4f && !doneBridge1)
            {
                GrabbableObject[] grabbables = Object.FindObjectsOfType<GrabbableObject>();
                foreach (GrabbableObject grabbable in grabbables)
                {
                    if (!grabbable.isInFactory && !grabbable.isHeld && !grabbable.isHeldByEnemy && !grabbable.isInElevator && !grabbable.isInShipRoom && grabbable.transform.parent != null)
                    {
                        SetGrabbableFall(grabbable);
                    }
                }
                doneBridge1 = true;
            }

            if (startTime2 != 0f && Time.realtimeSinceStartup - startTime2 > 1.4f && !doneBridge2)
            {
                GrabbableObject[] grabbables = Object.FindObjectsOfType<GrabbableObject>();
                foreach (GrabbableObject grabbable in grabbables)
                {
                    if (!grabbable.isInFactory && !grabbable.isHeld && !grabbable.isHeldByEnemy && !grabbable.isInElevator && !grabbable.isInShipRoom && grabbable.transform.parent != null)
                    {
                        SetGrabbableFall(grabbable);
                    }
                }
                doneBridge2 = true;
            }
        }

        public static void SetGrabbableFall(GrabbableObject grabbable)
        {
            Vector3 targetPos = grabbable.GetItemFloorPosition();
            if (!StartOfRound.Instance.shipBounds.bounds.Contains(targetPos))
            {
                targetPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(targetPos);
            }
            else
            {
                targetPos = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(targetPos);
            }
            if (Vector3.Distance(targetPos, grabbable.transform.parent.InverseTransformPoint(grabbable.transform.position)) < 0.1f) { return; }

            grabbable.startFallingPosition = grabbable.transform.parent.InverseTransformPoint(grabbable.transform.position);
            grabbable.fallTime = 0f;
            grabbable.hasHitGround = false;
            grabbable.reachedFloorTarget = false;
            grabbable.targetFloorPosition = targetPos;
        }
    }
}
