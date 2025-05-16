using System.Linq;
using HarmonyLib;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using WesleyMoonScripts.Components;
using Unity.Netcode;
using Steamworks.ServerList;
using static UnityEngine.Rendering.DebugUI;
using System.ComponentModel;
using static SelfSortingStorage.Cupboard.SmartMemory;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class NullItemPatches
    {
        public static GameObject itemReplacementPrefab;
        public static float startTime = 0f;

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

                            Scripts.NullItemScript.Instance.SpawnReplacementObjectClientRpc(component.gameObject.GetComponent<NetworkObject>(), value, component.transform.position, component.startFallingPosition, component.targetFloorPosition, component.itemUsedUp, component.transform.rotation);
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
    }
}
