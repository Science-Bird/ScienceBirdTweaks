using UnityEngine;
using HarmonyLib;
using System;
using Unity.Netcode;
using System.Collections.Generic;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class FixedShipObjectsPatches
    {
        static bool destroyCord = false;
        public static GameObject furniturePrefab;
        public static List<int> idBlacklist = new List<int>();
        public static int[] vanillaIDs = [5, 6, 9, 10, 12, 13, 14, 17, 18, 19, 20, 21, 22, 23];// most vanilla furniture items
        public static List<int> moddedIDs;
        public static string moddedListMode = "blacklist";

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
                ScienceBirdTweaks.Logger.LogDebug("Parenting suits to ship!");
                UnlockableSuit[] suits = UnityEngine.Object.FindObjectsOfType<UnlockableSuit>();
                foreach (UnlockableSuit suit in suits)
                {
                    GameObject suitObj = suit.gameObject;
                    NetworkObject suitNetworked = suitObj.GetComponent<NetworkObject>();
                    if (suitNetworked.TrySetParent(hangarShip.transform, worldPositionStays: true))
                    {
                        continue;
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
            // location of teleporter cord depends on whether fixed ship objects is being used, and whether its simplified logic is being used
            GameObject cord = GameObject.Find("/Teleporter(Clone)/ButtonContainer/LongCord") ?? GameObject.Find("/Environment/HangarShip/Furniture(Clone)/Teleporter(Clone)/ButtonContainer/LongCord") ?? GameObject.Find("/Environment/HangarShip/Teleporter(Clone)/ButtonContainer/LongCord");
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

            if (!ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.AlternateFixLogic.Value)// regular fixed furniture logic involves setting up a network object to contain furniture (within the ship), and parenting any furniture to this object upon spawn
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

        static void ParentFurniture(StartOfRound round, int index)
        {
            if (!round.IsServer) { return; }
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            if (hangarShip == null) { return; }

            if (!ScienceBirdTweaks.OnlyFixDefault.Value && ScienceBirdTweaks.ModdedListMode.Value != "Don't Use List" && ScienceBirdTweaks.ModdedUnlockableList.Value != "" && (moddedIDs == null || moddedIDs.Count <= 0))
            {
                moddedIDs = new List<int>();
                string[] unlockableNames = ScienceBirdTweaks.ModdedUnlockableList.Value.Replace(", ",",").Split(",");
                foreach (string name in unlockableNames)
                {
                    UnlockableItem targetUnlockable = round.unlockablesList.unlockables.Find(x => x.unlockableName.ToLower() == name.ToLower());
                    if (targetUnlockable != null)
                    {
                        moddedIDs.Add(round.unlockablesList.unlockables.IndexOf(targetUnlockable));
                    }
                }
            }

            if (ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.AlternateFixLogic.Value)// alternate/clientside logic just parents objects to the ship directly, without using the container object
            {
                if (ScienceBirdTweaks.OnlyFixDefault.Value && Array.IndexOf(vanillaIDs, index) == -1)
                {
                    return;
                }
                if (!ScienceBirdTweaks.OnlyFixDefault.Value && moddedIDs != null && moddedIDs.Count > 0)
                {
                    if (ScienceBirdTweaks.ModdedListMode.Value == "Whitelist" && !moddedIDs.Contains(index))
                    {
                        return;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Whitelisted index {index}!");
                    }
                    if (ScienceBirdTweaks.ModdedListMode.Value == "Blacklist" && moddedIDs.Contains(index))
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Blacklisted index {index}!");
                        return;
                    }
                }
                if (idBlacklist.Contains(index))
                {
                    return;
                }
                UnlockableItem unlockable = round.unlockablesList.unlockables[index];
                if (unlockable != null && unlockable.spawnPrefab && unlockable.prefabObject != null)
                {
                    GameObject gameObj = GameObject.Find(unlockable.prefabObject.name + "(Clone)");
                    if (gameObj.transform.parent != null && gameObj.transform.parent == hangarShip.transform)
                    {
                        return;
                    }
                    NetworkObject[] networkObjs = gameObj.GetComponentsInChildren<NetworkObject>();
                    foreach (NetworkObject networkObj in networkObjs)
                    {
                        if (!networkObj.IsSpawned)
                        {
                            ScienceBirdTweaks.Logger.LogError($"Network object not spawned yet, failing to parent! ({gameObj.name})");
                            return;
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
                            if (!idBlacklist.Contains(index))// make sure we don't try to parent the same object again that already failed
                            {
                                idBlacklist.Add(index);
                            }
                        }
                    }
                }
            }
            else// same but for normal logic with the furniture container
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
                if (ScienceBirdTweaks.OnlyFixDefault.Value && Array.IndexOf(vanillaIDs, index) == -1)
                {
                    return;
                }
                if (!ScienceBirdTweaks.OnlyFixDefault.Value && moddedIDs != null && moddedIDs.Count > 0)
                {
                    if (ScienceBirdTweaks.ModdedListMode.Value == "Whitelist" && !moddedIDs.Contains(index))
                    {
                        return;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Whitelisted index {index}!");
                    }
                    if (ScienceBirdTweaks.ModdedListMode.Value == "Blacklist" && moddedIDs.Contains(index))
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Blacklisted index {index}!");
                        return;
                    }
                }
                if (idBlacklist.Contains(index))
                {
                    return;
                }
                UnlockableItem unlockable = round.unlockablesList.unlockables[index];
                if (unlockable != null && unlockable.spawnPrefab && unlockable.prefabObject != null)
                {
                    GameObject gameObj = GameObject.Find(unlockable.prefabObject.name + "(Clone)");
                    if (gameObj.transform.parent != null && gameObj.transform.parent == furniture.transform)
                    {
                        return;
                    }
                    NetworkObject[] networkObjs = gameObj.GetComponentsInChildren<NetworkObject>();
                    foreach (NetworkObject networkObj in networkObjs)
                    {
                        if (!networkObj.IsSpawned)
                        {
                            ScienceBirdTweaks.Logger.LogError($"Network object not spawned yet, failing to parent! ({gameObj.name})");
                            return;
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
                            if (!idBlacklist.Contains(index))
                            {
                                idBlacklist.Add(index);
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
            if (unlockableID == 5 && !__instance.IsServer)// clients detect purchase of a teleporter and defer destroying the code until after it's loaded
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
        static void OnSpawn(StartOfRound __instance, int unlockableIndex)// only runs on server/host
        {
            if (ScienceBirdTweaks.FixedShipObjects.Value)
            {
                ParentFurniture(__instance, unlockableIndex);
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
        static void ResetParentedObjects(StartOfRound __instance)// if using the regular furniture fix logic, unparent all the furniture before a game over and reset. this should hopefully stop any issues with the game or other mods being confused about unlockables being in the wrong location
        {// this is the only reason I use the furniture container object in the first place
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

