using UnityEngine;
using HarmonyLib;
using System;
using System.Linq;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using static Unity.Collections.Unicode;
using System.Collections.Generic;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class FixedShipObjectsPatches
    {
        static bool destroyCord = false;
        public static GameObject furniturePrefab;
        public static List<int> idBlacklist = new List<int>();
        public static int[] vanillaIDs = [5, 6, 9, 10, 12, 13, 14, 17, 18, 19, 20, 21, 22, 23];

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.PositionSuitsOnRack))]
        [HarmonyPostfix]
        static void ParentSuits(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.FixedSuitRack.Value || !__instance.IsServer)
            {
                return;
            }
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            if (hangarShip != null)
            {
                UnlockableSuit[] suits = UnityEngine.Object.FindObjectsOfType<UnlockableSuit>();
                foreach (UnlockableSuit suit in suits)
                {
                    GameObject suitObj = suit.gameObject;
                    NetworkObject suitNetworked = suitObj.GetComponent<NetworkObject>();
                    if (suitNetworked.TrySetParent(hangarShip.transform, worldPositionStays: true))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Parented suit to ship!");
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogError("Failed to parent suit to ship!");
                    }
                }
            }
        }

        static void DestroyCord()
        {
            GameObject cord = GameObject.Find("/Teleporter(Clone)/ButtonContainer/LongCord") ?? GameObject.Find("/Environment/HangarShip/Furniture(Clone)/Teleporter(Clone)/ButtonContainer/LongCord");
            if (cord != null)
            {
                destroyCord = false;
                GameObject.Destroy(cord);
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializePrefab()
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.AlternateFixLogic.Value)
            {
                ScienceBirdTweaks.Logger.LogDebug("Initializing furniture holder!");
                furniturePrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("Furniture");
                NetworkManager.Singleton.AddNetworkPrefab(furniturePrefab);
            }
        }

        public static void SetupFurniture(StartOfRound round)
        {
            if (!round.IsServer) { return; }
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            if (hangarShip == null) { return; }

            if (!ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.AlternateFixLogic.Value)
            {
                GameObject furniture = UnityEngine.Object.Instantiate(furniturePrefab, Vector3.zero, Quaternion.identity);
                if (furniture != null && !furniture.GetComponent<NetworkObject>().IsSpawned)
                {
                    furniture.GetComponent<NetworkObject>().Spawn();
                    GameObject newFurniture = GameObject.Find("Furniture(Clone)");
                    if (newFurniture.GetComponent<NetworkObject>().TrySetParent(hangarShip.transform, worldPositionStays: true))
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Parented furniture holder to ship!");
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogError("Failed parenting furniture holder!");
                    }
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogDebug("Furniture network already spawned!");
                }
            }

        }

        static void ParentFurniture(StartOfRound round)
        {
            if (!round.IsServer) { return; }
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            if (hangarShip == null) { return; }

            if (ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.AlternateFixLogic.Value)
            {
                PlaceableShipObject[] furnitureObjects = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
                foreach (PlaceableShipObject obj in furnitureObjects)
                {
                    if (ScienceBirdTweaks.OnlyFixDefault.Value && Array.IndexOf(vanillaIDs, obj.unlockableID) == -1)
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Skipping non-default ID... {obj.unlockableID}");
                        continue;
                    }
                    if (idBlacklist.Contains(obj.unlockableID))
                    {
                        continue;
                    }
                    UnlockableItem unlockable = round.unlockablesList.unlockables[obj.unlockableID];
                    if (unlockable != null && unlockable.spawnPrefab)
                    {
                        if (obj.gameObject.GetComponentInParent<AutoParentToShip>())
                        {
                            GameObject gameObj = obj.GetComponentInParent<AutoParentToShip>().gameObject;
                            if (gameObj.transform.parent != null && gameObj.transform.parent == hangarShip.transform)
                            {
                                continue;
                            }
                            NetworkObject[] networkObjs = gameObj.GetComponentsInChildren<NetworkObject>();
                            foreach (NetworkObject networkObj in networkObjs)
                            {
                                if (!networkObj.IsSpawned)
                                {
                                    ScienceBirdTweaks.Logger.LogError($"Network object not spawned yet, failing to parent! ({gameObj.name})");
                                    continue;
                                }
                            }
                            NetworkObject networkedObj = gameObj.GetComponent<NetworkObject>();
                            if (networkedObj != null && networkedObj.IsSpawned)
                            {
                                if (networkedObj.TrySetParent(hangarShip.transform, worldPositionStays: true))
                                {
                                    ScienceBirdTweaks.Logger.LogDebug($"Parented furniture object {gameObj.name}!");
                                }
                                else
                                {
                                    ScienceBirdTweaks.Logger.LogError($"Failed to parent {gameObj.name}!");
                                    if (!idBlacklist.Contains(obj.unlockableID))
                                    {
                                        idBlacklist.Add(obj.unlockableID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                GameObject furniture = GameObject.Find("/Environment/HangarShip/Furniture(Clone)");
                if (furniture == null)
                {
                    SetupFurniture(round);
                }
                furniture = GameObject.Find("/Environment/HangarShip/Furniture(Clone)");
                if (furniture == null)
                {
                    ScienceBirdTweaks.Logger.LogError("Unable to resolve null furniture object!");
                    return;
                }

                PlaceableShipObject[] furnitureObjects = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
                foreach (PlaceableShipObject obj in furnitureObjects)
                {
                    if (ScienceBirdTweaks.OnlyFixDefault.Value && Array.IndexOf(vanillaIDs, obj.unlockableID) == -1)
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Skipping non-default ID... {obj.unlockableID}");
                        continue;
                    }
                    if (idBlacklist.Contains(obj.unlockableID))
                    {
                        continue;
                    }
                    UnlockableItem unlockable = round.unlockablesList.unlockables[obj.unlockableID];
                    if (unlockable != null && unlockable.spawnPrefab)
                    {
                        if (obj.gameObject.GetComponentInParent<AutoParentToShip>())
                        {
                            GameObject gameObj = obj.GetComponentInParent<AutoParentToShip>().gameObject;
                            if (gameObj.transform.parent != null && gameObj.transform.parent == furniture.transform)
                            {
                                continue;
                            }
                            NetworkObject[] networkObjs = gameObj.GetComponentsInChildren<NetworkObject>();
                            foreach (NetworkObject networkObj in networkObjs)
                            {
                                if (!networkObj.IsSpawned)
                                {
                                    ScienceBirdTweaks.Logger.LogError($"Network object not spawned yet, failing to parent! ({gameObj.name})");
                                    continue;
                                }
                            }
                            NetworkObject networkedObj = gameObj.GetComponent<NetworkObject>();
                            if (networkedObj != null && networkedObj.IsSpawned && round.IsServer)
                            {
                                if (networkedObj.TrySetParent(furniture.transform, worldPositionStays: true))
                                {
                                    ScienceBirdTweaks.Logger.LogDebug($"Parented furniture object {gameObj.name}!");
                                }
                                else
                                {
                                    ScienceBirdTweaks.Logger.LogError($"Failed to parent {gameObj.name}!");
                                    if (!idBlacklist.Contains(obj.unlockableID))
                                    {
                                        idBlacklist.Add(obj.unlockableID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableClientRpc))]
        [HarmonyPostfix]
        static void OnBuy(StartOfRound __instance, int unlockableID)
        {
            if (unlockableID == 5 && !__instance.IsServer)
            {
                destroyCord = true;
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        static void OnUpdate(StartOfRound __instance)
        {
            if (destroyCord)
            {
                DestroyCord();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        [HarmonyPostfix]
        static void OnSpawn(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.FixedShipObjects.Value)
            {
                ParentFurniture(__instance);
            }
            if (ScienceBirdTweaks.RemoveTeleporterCord.Value)
            {
                DestroyCord();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPostfix]
        static void OnClientSync(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.RemoveTeleporterCord.Value)
            {
                DestroyCord();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShipFurniture))]
        [HarmonyPrefix]
        static void ResetParentedObjects(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.FixedShipObjects.Value || !__instance.IsServer || ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.AlternateFixLogic.Value)
            {
                return;
            }
            GameObject furniture = GameObject.Find("/Environment/HangarShip/Furniture(Clone)");
            if (furniture != null)
            {
                AutoParentToShip[] furnitureObjects = furniture.GetComponentsInChildren<AutoParentToShip>();
                foreach (AutoParentToShip obj in furnitureObjects)
                {
                    GameObject gameObj = obj.gameObject;
                    if (gameObj.transform.parent != null && gameObj.transform.parent == furniture.transform)
                    {
                        NetworkObject networkObj = gameObj.GetComponent<NetworkObject>();
                        if (networkObj != null)
                        {
                            Transform nullTransform = null;
                            networkObj.TrySetParent(nullTransform, worldPositionStays: true);
                            ScienceBirdTweaks.Logger.LogDebug($"Un-parented furniture object {gameObj.name}!");
                        }
                    }
                }
                furniture.GetComponent<NetworkObject>().Despawn();
                UnityEngine.Object.Destroy(furniture);
            }
        }
    }
}

