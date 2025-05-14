using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class MaskDropPatches
    {
        public static GameObject tragedyPrefab;
        public static GameObject comedyPrefab;
        public static GameObject oniPrefab;
        public static GameObject maskScriptPrefab;
        public static bool patchingMask = false;
        public static Dictionary<int, (Vector3, Quaternion, int)> maskBuffer;


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializePrefab()
        {
            if (ScienceBirdTweaks.DropMasks.Value && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                maskScriptPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("MaskDropScript");
                NetworkManager.Singleton.AddNetworkPrefab(maskScriptPrefab);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        public static void InitializeAssets(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DropMasks.Value || ScienceBirdTweaks.ClientsideMode.Value)
                return;

            FindMaskPrefabs(true);
            maskBuffer = new Dictionary<int, (Vector3, Quaternion, int)>();
            MaskDropScript.activeMasks = new List<MaskInstance>();
        }

        private static void FindMaskPrefabs(bool doOniCheck = false)
        {
            if (tragedyPrefab == null)
            {
                GrabbableObject[] tragedySet = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is HauntedMaskItem && x.itemProperties != null && x.itemProperties.itemName == "Tragedy").ToArray();
                foreach (GrabbableObject tragedy in tragedySet)
                {
                    if (tragedy.gameObject.GetComponent<NetworkObject>() && tragedy.gameObject.GetComponent<NetworkObject>().PrefabIdHash != 0)
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Found tragedy!");
                        tragedyPrefab = tragedy.gameObject;
                        break;
                    }
                }
            }
            if (comedyPrefab == null)
            {
                GrabbableObject[] comedySet = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is HauntedMaskItem && x.itemProperties != null && x.itemProperties.itemName == "Comedy").ToArray();
                foreach (GrabbableObject comedy in comedySet)
                {
                    if (comedy.gameObject.GetComponent<NetworkObject>() && comedy.gameObject.GetComponent<NetworkObject>().PrefabIdHash != 0)
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Found comedy!");
                        comedyPrefab = comedy.gameObject;
                        break;
                    }
                }
            }
            if (doOniCheck && oniPrefab == null)
            {
                GrabbableObject[] oniSet = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is HauntedMaskItem && x.itemProperties != null && x.itemProperties.itemName == "OniMask").ToArray();
                foreach (GrabbableObject oni in oniSet)
                {
                    if (oni.gameObject.GetComponent<NetworkObject>() && oni.gameObject.GetComponent<NetworkObject>().PrefabIdHash != 0)
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Found oni!");
                        oniPrefab = oni.gameObject;
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillEnemy))]
        [HarmonyPostfix]
        static void OnMaskDeath(MaskedPlayerEnemy __instance)
        {
            if (!ScienceBirdTweaks.DropMasks.Value || !__instance.gameObject.GetComponentInChildren<RandomPeriodicAudioPlayer>() || ScienceBirdTweaks.ClientsideMode.Value)
                return;

            GameObject mask = __instance.gameObject.GetComponentsInChildren<RandomPeriodicAudioPlayer>().Where(x => x.gameObject.activeInHierarchy).First().gameObject;// this is the mask worn on the mask enemy
            GameObject? maskMesh = mask.transform.Find("Mesh") ? mask.transform.Find("Mesh").gameObject : null;

            if (maskMesh == null && mask.name == "HeadOni")
            {
                maskMesh = mask;
            }

            if (maskMesh == null || maskMesh.GetComponent<MeshRenderer>() == null)
            {
                ScienceBirdTweaks.Logger.LogDebug($"Null mesh! {maskMesh}");
                return;
            }

            GameObject? maskPrefab = null;
            MaskDropScript maskScript = Object.FindObjectOfType<MaskDropScript>();
            if (__instance.IsServer)
            {
                MeshRenderer maskRenderer = maskMesh.GetComponent<MeshRenderer>();
                switch (maskRenderer.material.name)
                {
                    case "ComedyMaskMat (Instance)":
                        FindMaskPrefabs();
                        //ScienceBirdTweaks.Logger.LogDebug("Selected comedy!");
                        maskPrefab = comedyPrefab;
                        break;
                    case "TragedyMaskMat (Instance)":
                        FindMaskPrefabs();
                        //ScienceBirdTweaks.Logger.LogDebug("Selected tragedy!");
                        maskPrefab = tragedyPrefab;
                        break;
                    case "oni (Instance)":
                        FindMaskPrefabs(true);
                        //ScienceBirdTweaks.Logger.LogDebug("Selected oni!");
                        maskPrefab = oniPrefab;
                        break;
                }
                if (maskScript == null)
                {
                    GameObject maskScriptObj = Object.Instantiate(maskScriptPrefab, Vector3.zero, Quaternion.identity);
                    maskScriptObj.GetComponent<NetworkObject>().Spawn();
                }
            }
            maskScript = Object.FindObjectOfType<MaskDropScript>();
            if (maskScript != null)
            {
                maskScript.PrepareMaskDropCoroutine(maskPrefab, maskMesh);// clients don't have access to maskPrefab, so it will always be passed null except on server
            }
        }

        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.DiscardItem))]
        [HarmonyPrefix]
        static bool MaskDropSyncClient(HauntedMaskItem __instance)// this is artificially induced by mask script by exploiting an existing rpc, and will only run on clients
        {
            if (!ScienceBirdTweaks.DropMasks.Value || maskBuffer == null || __instance.GetComponent<NetworkObject>() == null || ScienceBirdTweaks.ClientsideMode.Value) { return true; }

            if (patchingMask && __instance.previousPlayerHeldBy == null && maskBuffer.TryGetValue((int)__instance.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
            {
                MaskDropSync(__instance, (int)__instance.GetComponent<NetworkObject>().NetworkObjectId, value.Item1, value.Item2, value.Item3);
                return false;
            }
            return true;
        }

        public static void MaskDropSync(HauntedMaskItem maskGrabbable, int maskId, Vector3 targetPos, Quaternion targetRot, int scrapValue)// sync mask properties so it should stay in place when spawned
        {
            ScienceBirdTweaks.Logger.LogDebug($"Spawning mask with pos: {targetPos}, rot: {targetRot.eulerAngles}");
            maskGrabbable.gameObject.transform.position = targetPos;
            maskGrabbable.gameObject.transform.rotation = targetRot;
            maskGrabbable.fallTime = 1.1f;
            maskGrabbable.startFallingPosition = maskGrabbable.gameObject.transform.position;
            maskGrabbable.targetFloorPosition = maskGrabbable.gameObject.transform.position;
            maskGrabbable.hasHitGround = true;
            maskGrabbable.reachedFloorTarget = true;
            maskGrabbable.SetScrapValue(scrapValue);
            if (maskBuffer.Count <= 1)// only stop checking if we're the only one in queue
            {
                patchingMask = false;
            }
        }

        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.LateUpdate))]
        [HarmonyPostfix]
        static void FixMaskPosition(HauntedMaskItem __instance)// make sure mask position is not corrected by grabbable update logic
        {
            if (!ScienceBirdTweaks.DropMasks.Value || maskBuffer == null || __instance.GetComponent<NetworkObject>() == null || ScienceBirdTweaks.ClientsideMode.Value) { return; }

            if (__instance.previousPlayerHeldBy == null && __instance.playerHeldBy == null && maskBuffer.TryGetValue((int)__instance.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
            {
                __instance.gameObject.transform.position = value.Item1;
                __instance.gameObject.transform.rotation = value.Item2;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectClientRpc))]
        [HarmonyPrefix]
        static void MaskPickup(PlayerControllerB __instance, bool grabValidated, NetworkObjectReference grabbedObject)// mask is still registered in buffer even after spawning so its position can be updated, but when picked up it should be removed from the buffer
        {
            if (ScienceBirdTweaks.DropMasks.Value && grabValidated && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                NetworkObjectReference tempGrabbed = grabbedObject;
                if (tempGrabbed.TryGet(out var networkObject))
                {
                    GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                    if (component.gameObject.GetComponent<HauntedMaskItem>() && maskBuffer.TryGetValue((int)component.GetComponent<NetworkObject>().NetworkObjectId, out (Vector3, Quaternion, int) value))
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Removing mask from buffer! {maskBuffer.Count}, {patchingMask}");
                        maskBuffer.Remove((int)component.GetComponent<NetworkObject>().NetworkObjectId);
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
