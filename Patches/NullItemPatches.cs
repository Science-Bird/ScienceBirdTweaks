using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameNetcodeStuff;
using HarmonyLib;
using ScienceBirdTweaks.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class NullItemPatches
    {
        public static GameObject itemReplacementPrefab;
        public static float startTime = 0f;
        private static bool spawned = false;
        public static bool triggered = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void RegisterPrefab(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ReplaceNullItems.Value && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                itemReplacementPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("SBTNullFixScript");
                NetworkManager.Singleton.AddNetworkPrefab(itemReplacementPrefab);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        static void SpawnScript(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ReplaceNullItems.Value && !ScienceBirdTweaks.ClientsideMode.Value && __instance.IsServer && !GameObject.Find("SBTNullFixScript(Clone)"))
            {
                GameObject replaceScript = UnityEngine.Object.Instantiate(itemReplacementPrefab, Vector3.zero, Quaternion.identity);
                replaceScript.GetComponent<NetworkObject>().Spawn();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ChangeLevel))]
        [HarmonyPostfix]
        static void OnChangeLevel(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ReplaceNullItems.Value && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                startTime = Time.realtimeSinceStartup;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        static void FixInterval(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.ReplaceNullItems.Value && !ScienceBirdTweaks.ClientsideMode.Value)
            {
                if (startTime != 0f && Time.realtimeSinceStartup - startTime > 0.5f)
                {
                    startTime = 0f;
                    FixGrabbables(__instance);
                }
            }
        }

        static void FixGrabbables(StartOfRound round)
        {
            GrabbableObject[] brokenGrabbables = UnityEngine.Object.FindObjectsOfType<GrabbableObject>().Where(x => x.itemProperties == null).ToArray();
            if (brokenGrabbables.Length > 0)
            {
                if (round.IsServer && !GameObject.Find("SBTNullFixScript(Clone)"))
                {
                    GameObject replaceScript = UnityEngine.Object.Instantiate(itemReplacementPrefab, Vector3.zero, Quaternion.identity);
                    replaceScript.GetComponent<NetworkObject>().Spawn();
                }
                Dictionary<string, GrabbableObject> brokenNameDict = new Dictionary<string, GrabbableObject>();
                foreach (GrabbableObject grabbable in brokenGrabbables)
                {
                    string name = Regex.Replace(grabbable.gameObject.name, "\\(Clone\\)$", "");
                    brokenNameDict.Add(name, grabbable);
                }
                Item[] replacementItems = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.spawnPrefab != null && brokenNameDict.Keys.Contains(x.spawnPrefab.name) && x.spawnPrefab.GetComponent<NetworkObject>().PrefabIdHash != 0).ToArray();
                foreach (Item properties in replacementItems)
                {
                    if (brokenNameDict.TryGetValue(properties.spawnPrefab.name, out GrabbableObject grabbable))
                    {
                        if (round.IsServer)
                        {
                            Transform parent = StartOfRound.Instance.elevatorTransform;
                            GameObject newObject = UnityEngine.Object.Instantiate(properties.spawnPrefab, grabbable.transform.position, Quaternion.identity);
                            GrabbableObject component = newObject.GetComponent<GrabbableObject>();
                            component.itemUsedUp = grabbable.itemUsedUp;
                            int value = -1;
                            if (component.itemProperties.isScrap)
                            {
                                value = grabbable.scrapValue;
                                component.SetScrapValue(value);
                            }
                            component.isInShipRoom = true;
                            component.isInElevator = true;
                            component.isInFactory = false;
                            component.gameObject.GetComponent<NetworkObject>().Spawn();
                            component.gameObject.transform.SetParent(parent, true);

                            component.transform.position += Vector3.up * 0.5f;
                            BridgePatches.SetGrabbableFall(component);

                            NullItemScript.Instance.SpawnReplacementObjectClientRpc(component.gameObject.GetComponent<NetworkObject>(), value, component.transform.position, component.startFallingPosition, component.targetFloorPosition, component.itemUsedUp, component.transform.rotation, true, true, false);
                        }
                        int slot = -1;
                        if ((grabbable.isHeld || grabbable.isPocketed) && grabbable.playerHeldBy != null)
                        {
                            slot = System.Array.IndexOf(grabbable.playerHeldBy.ItemSlots, grabbable);
                            if (slot >= 0)
                            {
                                grabbable.playerHeldBy.DestroyItemInSlot(slot);
                            }
                        }
                        else
                        {
                            Object.Destroy(grabbable.gameObject);
                        }
                        brokenNameDict.Remove(properties.spawnPrefab.name);
                    }
                }
            }

        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        static void StartTriggerReset(StartOfRound __instance)
        {
            triggered = false;
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        [HarmonyPrefix]
        static void ItemErrorDebug(GrabbableObject __instance)
        {
            if (!ScienceBirdTweaks.FixNaNColliders.Value || triggered || ScienceBirdTweaks.ClientsideMode.Value)
            {
                return;
            }
            if (IsInvalidTransform(__instance.transform))
            {
                triggered = true;
                ScienceBirdTweaks.Logger.LogError($"-----------------------------------------------------------------");
                ScienceBirdTweaks.Logger.LogError($"COLLIDER ERRORS DETECTED ON: {__instance.gameObject.name} (path: {GetObjectPath(__instance.gameObject)})");
                ScienceBirdTweaks.Logger.LogError($"-----------------------------------------------------------------");
                //ScienceBirdTweaks.Logger.LogError($"ID: {__instance.NetworkObjectId}");
                Collider[] allColliders = __instance.gameObject.GetComponentsInChildren<Collider>();
                foreach (Collider collider in allColliders)
                {
                    ScienceBirdTweaks.Logger.LogWarning($"Check for {GetObjectPath(collider.gameObject)} - Transform corrupt: {IsInvalidTransform(collider.gameObject.transform)}; Collider corrupt: {IsInvalidCollider(collider)}");
                    //ScienceBirdTweaks.Logger.LogWarning($"POS: {collider.gameObject.transform.position.x}, {collider.gameObject.transform.position.y}, {collider.gameObject.transform.position.z}");
                }
                ScienceBirdTweaks.Logger.LogInfo($"Attempting fix...");

                if (NullItemScript.Instance != null)
                {
                    NullItemScript.Instance.RequestReplacementObjectServerRpc(__instance.NetworkObject);
                    
                }
            }
        }

        private static bool IsInvalidTransform(Transform itemTransform)
        {
            return float.IsNaN(itemTransform.position.x) || float.IsInfinity(itemTransform.position.x) || float.IsNaN(itemTransform.position.y) || float.IsInfinity(itemTransform.position.y) || float.IsNaN(itemTransform.position.z) || float.IsInfinity(itemTransform.position.z)
                || float.IsNaN(itemTransform.localScale.x) || float.IsInfinity(itemTransform.localScale.x) || float.IsNaN(itemTransform.localScale.y) || float.IsInfinity(itemTransform.localScale.y) || float.IsNaN(itemTransform.localScale.z) || float.IsInfinity(itemTransform.localScale.z);
        }

        private static bool IsInvalidCollider(Collider itemCollider)
        {
            return float.IsNaN(itemCollider.bounds.max.x) || float.IsInfinity(itemCollider.bounds.max.x) || float.IsNaN(itemCollider.bounds.max.y) || float.IsInfinity(itemCollider.bounds.max.y) || float.IsNaN(itemCollider.bounds.max.z) || float.IsInfinity(itemCollider.bounds.max.z)
                || float.IsNaN(itemCollider.bounds.min.x) || float.IsInfinity(itemCollider.bounds.min.x) || float.IsNaN(itemCollider.bounds.min.y) || float.IsInfinity(itemCollider.bounds.min.y) || float.IsNaN(itemCollider.bounds.min.z) || float.IsInfinity(itemCollider.bounds.min.z);
        }

        private static string GetObjectPath(GameObject obj)
        {
            StringBuilder path = new StringBuilder(obj.name);
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }
    }
}
