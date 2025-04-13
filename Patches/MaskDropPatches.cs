using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using Steamworks.ServerList;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class MaskDropPatches
    {
        public static GameObject tragedyPrefab;
        public static GameObject comedyPrefab;
        public static GameObject maskScriptPrefab;
        public static bool patchingMask = false;
        public static Dictionary<int, (Vector3, Quaternion, int)> maskBuffer;
        public static System.Random maskRandom;


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializePrefab()
        {
            maskScriptPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("MaskDropScript");
            NetworkManager.Singleton.AddNetworkPrefab(maskScriptPrefab);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        public static void InitializeAssets(StartOfRound __instance)
        {
            if (tragedyPrefab == null || comedyPrefab == null)
            {
                Item tragedy = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Tragedy").First();
                Item comedy = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Comedy").First();
                if (tragedy != null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Found tragedy!");
                    tragedyPrefab = tragedy.spawnPrefab;
                }
                if (comedy != null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Found comedy!");
                    comedyPrefab = comedy.spawnPrefab;
                }
            }
            maskBuffer = new Dictionary<int, (Vector3, Quaternion, int)>();
            MaskDropScript.activeMasks = new List<MaskInstance>();
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillEnemy))]
        [HarmonyPostfix]
        static void MaskTest(MaskedPlayerEnemy __instance)
        {
            ScienceBirdTweaks.Logger.LogInfo($"Killing mask!");
            GameObject mask = __instance.gameObject.GetComponentsInChildren<RandomPeriodicAudioPlayer>().Where(x => x.gameObject.activeInHierarchy).First().gameObject;
            GameObject maskMesh = mask.transform.Find("Mesh").gameObject;
            GameObject? maskPrefab = null;
            MaskDropScript maskScript = Object.FindObjectOfType<MaskDropScript>();
            if (__instance.IsServer)
            {
                if (mask.name.Contains("Tragedy"))
                {
                    if (tragedyPrefab == null)
                    {
                        Item tragedy = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Tragedy").First();
                        if (tragedy != null)
                        {
                            ScienceBirdTweaks.Logger.LogDebug("Found tragedy!");
                            tragedyPrefab = tragedy.spawnPrefab;
                        }
                    }
                    maskPrefab = tragedyPrefab;
                }
                else if (mask.name.Contains("Comedy"))
                {
                    if (comedyPrefab == null)
                    {
                        Item comedy = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Comedy").First();
                        if (comedy != null)
                        {
                            ScienceBirdTweaks.Logger.LogDebug("Found comedy!");
                            comedyPrefab = comedy.spawnPrefab;
                        }
                    }
                    maskPrefab = comedyPrefab;
                }
                if (maskScript == null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Spawning script...");
                    GameObject maskScriptObj = Object.Instantiate(maskScriptPrefab, Vector3.zero, Quaternion.identity);
                    maskScriptObj.GetComponent<NetworkObject>().Spawn();
                }
            }
            maskScript = Object.FindObjectOfType<MaskDropScript>();
            if (maskScript != null)
            {
                ScienceBirdTweaks.Logger.LogDebug($"Found mask script! {maskMesh.transform.position}, {maskMesh.transform.eulerAngles}");
                maskScript.PrepareMaskDropCoroutine(maskPrefab, maskMesh);
            }
        }

        /*
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItemClientRpc))]
        [HarmonyPrefix]
        static bool MaskDropSyncServer(GrabbableObject __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug($"CLIENT RPC");
            if (patchingMask && __instance.GetComponent<HauntedMaskItem>() && __instance.GetComponent<HauntedMaskItem>().previousPlayerHeldBy == null && maskBuffer.TryGetValue((int)__instance.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion) value))
            {
                MaskDropSync(__instance.GetComponent<HauntedMaskItem>(), (int)__instance.GetComponent<NetworkObject>().NetworkObjectId, value.Item1, value.Item2);
                __instance.DiscardItem();
                return false;
            }
            return true;
        }
        */

        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.DiscardItem))]
        [HarmonyPrefix]
        static bool MaskDropSyncClient(HauntedMaskItem __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug($"MAIN FUNCTION");
            if (patchingMask && __instance.previousPlayerHeldBy == null && maskBuffer.TryGetValue((int)__instance.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
            {
                MaskDropSync(__instance, (int)__instance.GetComponent<NetworkObject>().NetworkObjectId, value.Item1, value.Item2, value.Item3);
                return false;
            }
            return true;
        }

        public static void MaskDropSync(HauntedMaskItem maskGrabbable, int maskId, Vector3 targetPos, Quaternion targetRot, int scrapValue)
        {
            ScienceBirdTweaks.Logger.LogDebug($"Found a mask! {targetPos}, {targetRot.eulerAngles}");
            maskGrabbable.gameObject.transform.position = targetPos + new Vector3(0f, 0.6f, 0f);
            maskGrabbable.gameObject.transform.rotation = targetRot;
            maskGrabbable.fallTime = 1.1f;
            maskGrabbable.startFallingPosition = maskGrabbable.gameObject.transform.position;
            maskGrabbable.targetFloorPosition = maskGrabbable.gameObject.transform.position;
            maskGrabbable.hasHitGround = true;
            maskGrabbable.reachedFloorTarget = true;
            maskGrabbable.SetScrapValue(scrapValue);
            if (maskBuffer.Count <= 1)
            {
                patchingMask = false;
            }
        }

        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.LateUpdate))]
        [HarmonyPostfix]
        static void FixMaskPosition(HauntedMaskItem __instance)
        {
            if (__instance.previousPlayerHeldBy == null && __instance.playerHeldBy == null && maskBuffer.TryGetValue((int)__instance.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
            {
                __instance.gameObject.transform.position = value.Item1;
                __instance.gameObject.transform.rotation = value.Item2;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectClientRpc))]
        [HarmonyPrefix]
        static void MaskPickup(PlayerControllerB __instance, bool grabValidated, NetworkObjectReference grabbedObject)
        {
            if (grabValidated)
            {
                ScienceBirdTweaks.Logger.LogDebug("Detected valid grab!");
                NetworkObjectReference tempGrabbed = grabbedObject;
                if (tempGrabbed.TryGet(out var networkObject))
                {
                    ScienceBirdTweaks.Logger.LogDebug("Found grabbable component!");
                    GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                    if (component.gameObject.GetComponent<HauntedMaskItem>() && maskBuffer.TryGetValue((int)component.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Removing mask from buffer!");
                        maskBuffer.Remove((int)__instance.GetComponent<NetworkObject>().NetworkObjectId);
                        if (maskBuffer.Count <= 0)
                        {
                            patchingMask = false;
                        }
                    }
                }
            }
        }
    }
}
