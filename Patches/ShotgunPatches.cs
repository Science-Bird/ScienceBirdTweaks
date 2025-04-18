using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using Steamworks.Ugc;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ShotgunPatches
    {
        public static GameObject shellPrefab;
        public static string safetyOnText;
        public static string safetyOffText;
        public static string reloadString;
        public static string ammoString0;
        public static string ammoString1;
        public static string ammoString2;
        public static bool unloadEnabled = false;
        public static bool showAmmo = true;
        public static bool holdingDown = false;
        public static float startTime;
        private static bool shellRegistered = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            Item shotgun = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Shotgun").First();
            if (shotgun != null)
            {
                if (ScienceBirdTweaks.PickUpGunOrbit.Value)
                {
                    shotgun.canBeGrabbedBeforeGameStart = true;
                }
                if (shotgun.toolTips != null && shotgun.toolTips.Length == 3)
                {
                    safetyOnText = ScienceBirdTweaks.SafetyOnString.Value + " " + shotgun.toolTips[2].Remove(0, shotgun.toolTips[2].IndexOf(":"));
                    safetyOffText = ScienceBirdTweaks.SafetyOffString.Value + " " + shotgun.toolTips[2].Remove(0, shotgun.toolTips[2].IndexOf(":"));
                    reloadString = "Reload" + " " + shotgun.toolTips[1].Remove(0, shotgun.toolTips[1].IndexOf(":"));
                    ammoString0 = "( )( ) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                    ammoString1 = "(O)( ) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                    ammoString2 = "(O)(O) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                }
                else
                {
                    safetyOnText = ScienceBirdTweaks.SafetyOnString.Value + " : [Q]";
                    safetyOffText = ScienceBirdTweaks.SafetyOffString.Value + " : [Q]";
                    reloadString = "Reload" + " : [E]";
                    ammoString0 = "( )( ) Fire" + " : [RMB]";
                    ammoString1 = "(O)( ) Fire" + " : [RMB]";
                    ammoString2 = "(O)(O) Fire" + " : [RMB]";
                }
            }
            else
            {
                safetyOnText = ScienceBirdTweaks.SafetyOnString.Value + " : [Q]";
                safetyOffText = ScienceBirdTweaks.SafetyOffString.Value + " : [Q]";
                reloadString = "Reload" + " : [E]";
                ammoString0 = "( )( ) Fire" + " : [RMB]";
                ammoString1 = "(O)( ) Fire" + " : [RMB]";
                ammoString2 = "(O)(O) Fire" + " : [RMB]";
            }
            
            if (ScienceBirdTweaks.PickUpShellsOrbit.Value)
            {
                Item shell = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Ammo").First();
                if (shell != null)
                {
                    shell.canBeGrabbedBeforeGameStart = true;
                }
            }

            unloadEnabled = ScienceBirdTweaks.UnloadShells.Value;
            showAmmo = ScienceBirdTweaks.ShowAmmo.Value;

            if (!ScienceBirdTweaks.UnloadShells.Value) { return; }

            NutcrackerEnemyAI nutcracker = Resources.FindObjectsOfTypeAll<NutcrackerEnemyAI>().First();
            if (nutcracker != null)
            {
                shellPrefab = nutcracker.shotgunShellPrefab;
                if (shellPrefab.GetComponent<NetworkObject>().PrefabIdHash == 0)
                {
                    ScienceBirdTweaks.Logger.LogDebug("No shell found on initialization!");
                    shellRegistered = false;
                    if (ScienceBirdTweaks.xuPresent || ScienceBirdTweaks.ForceRegisterShells.Value)
                    {
                        ScienceBirdTweaks.Logger.LogInfo("Manually registering shell prefabs with network manager!");
                        NetworkManager.Singleton.AddNetworkPrefab(shellPrefab);
                        if (shellPrefab.GetComponent<NetworkObject>().PrefabIdHash != 0)
                        {
                            shellRegistered = true;
                        }
                    }
                }
                else
                {
                    shellRegistered = true;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ShellPrefabCheck(StartOfRound __instance, string sceneName)// some mods cause the network registration of this prefab to be delayed, so it's double-checked here
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            if (sceneName == "SampleSceneRelay")
            {
                if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(shellPrefab))
                {
                    NutcrackerEnemyAI nutcracker = Resources.FindObjectsOfTypeAll<NutcrackerEnemyAI>().First();
                    if (nutcracker != null)
                    {
                        shellPrefab = nutcracker.shotgunShellPrefab;
                        if (shellPrefab.GetComponent<NetworkObject>().PrefabIdHash == 0)
                        {
                            ScienceBirdTweaks.Logger.LogWarning("No shell found on load!");
                            shellRegistered = false;
                        }
                        else
                        {
                            shellRegistered = true;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.SetControlTipsForItem))]
        [HarmonyPostfix]
        public static void TooltipSet(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy) { return; }
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.SetSafetyControlTip))]
        [HarmonyPostfix]
        public static void SafetySet(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy) { return; }
            string changeTo = ((!__instance.safetyOn) ? safetyOffText : safetyOnText);
            if (__instance.IsOwner)
            {
                ScienceBirdTweaks.Logger.LogDebug($"Safety: {__instance.safetyOn}, setting text to {changeTo}");
                int num = 3;
                if (ScienceBirdTweaks.UnloadShells.Value && __instance.shellsLoaded <= 0 && __instance.FindAmmoInInventory() == -1)
                {// in-case tooltips have been shifted up
                    num = 2;
                }
                HUDManager.Instance.ChangeControlTip(num, changeTo);
            }
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsClientRpc))]
        [HarmonyPostfix]
        public static void PostReload(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy || __instance.playerHeldBy == null) { return; }
            ScienceBirdTweaks.Logger.LogDebug("Reload ended, updating tooltip!");
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
        [HarmonyPostfix]
        public static void PostShot(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy || __instance.playerHeldBy == null) { return; }
            ScienceBirdTweaks.Logger.LogDebug("Shot fired, updating tooltip!");
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SwitchToItemSlot))]
        [HarmonyPostfix]
        public static void OnSwitch(PlayerControllerB __instance, GrabbableObject fillSlotWithItem)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            ShotgunItem? shotgun = null;
            if (fillSlotWithItem != null && fillSlotWithItem.GetComponent<ShotgunItem>())
            {
                shotgun = fillSlotWithItem.GetComponent<ShotgunItem>();
            }
            else if (__instance.currentlyHeldObjectServer != null && __instance.currentlyHeldObjectServer.GetComponent<ShotgunItem>())
            {
                shotgun = __instance.currentlyHeldObjectServer.GetComponent<ShotgunItem>();
            }
            if (shotgun != null)
            {
                TooltipUpdate(shotgun);
            }
        }

        public static void TooltipUpdate(ShotgunItem shotgun)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || shotgun.isHeldByEnemy) { return; }
            if (shotgun.IsOwner)
            {
                string[] toolTips = shotgun.itemProperties.toolTips;
                if (showAmmo)
                {
                    switch (shotgun.shellsLoaded)
                    {
                        case 0:
                            toolTips[0] = ammoString0;
                            break;
                        case 1:
                            toolTips[0] = ammoString1;
                            break;
                        case 2:
                            toolTips[0] = ammoString2;
                            break;
                    }
                }
                if (shotgun.safetyOn)
                {
                    toolTips[2] = safetyOnText;
                }
                else
                {
                    toolTips[2] = safetyOffText;
                }
                if (unloadEnabled)
                {
                    if (shotgun.FindAmmoInInventory() != -1 && shotgun.shellsLoaded < 2)
                    {
                        toolTips[1] = reloadString;
                    }
                    else if (shotgun.shellsLoaded > 0)
                    {
                        toolTips[1] = "Eject shells : [Hold E]";
                    }
                    else// remove 2nd tooltip and shift 3rd one up
                    {
                        toolTips[1] = toolTips[2];
                        toolTips[2] = "";
                    }
                }
                HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, shotgun.itemProperties);
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemInteractLeftRightOnClient))]
        [HarmonyPrefix]
        static bool InteractPrefix(GrabbableObject __instance, bool right)// this exists to interrupt the usual interaction event if the eject requirements are met, this is so the eject procedure can do some client-side checks and do the hold event before starting synced interaction with other clients
        {
            if (!ScienceBirdTweaks.ShotgunMasterDisable.Value && unloadEnabled && __instance.GetComponent<ShotgunItem>() && right && (__instance.GetComponent<ShotgunItem>().FindAmmoInInventory() == -1 || __instance.GetComponent<ShotgunItem>().shellsLoaded >= 2) && __instance.GetComponent<ShotgunItem>().shellsLoaded > 0)
            {
                    if (__instance.IsOwner && __instance.isHeld && !__instance.isPocketed && HUDManager.Instance.holdFillAmount <= 0f && __instance.playerHeldBy.cursorTip.text == "")// make sure player isn't doing some other kind of ongoing interaction
                {
                    LocalInteract(__instance.GetComponent<ShotgunItem>(), right);
                }
                else
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"ABORTING HOLD {__instance.IsOwner}, {__instance.isHeld}, {HUDManager.Instance.holdFillAmount <= 0f}, {__instance.playerHeldBy.cursorTip.text == ""}");
                }
                // this inner failure will still return false (halting the interaction), because the basic eject criteria were met, we shouldn't try to do any interaction at all

                return false;
            }
            //ScienceBirdTweaks.Logger.LogDebug($"ABORTING HOLD {__instance.GetComponent<ShotgunItem>()}, {right}, {__instance.GetComponent<ShotgunItem>().FindAmmoInInventory() == -1 || __instance.GetComponent<ShotgunItem>().shellsLoaded >= 2}, {__instance.GetComponent<ShotgunItem>().shellsLoaded > 0}");
            return true;
        }


        static void LocalInteract(ShotgunItem shotgun, bool right)// initialize local hold event
        {
            if (unloadEnabled && shotgun.IsOwner && shotgun.isHeld && !shotgun.isPocketed && HUDManager.Instance.holdFillAmount <= 0f && shotgun.playerHeldBy.cursorTip.text == "" && right && (shotgun.FindAmmoInInventory() == -1 || shotgun.shellsLoaded >= 2) && shotgun.shellsLoaded > 0)
            {
                holdingDown = true;
                startTime = Time.realtimeSinceStartup;
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void AttemptInteract(ShotgunItem __instance)// make sure button is held for 1 second
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy) { return; }

            if (holdingDown && __instance.playerHeldBy != null && __instance.isHeld && !__instance.isPocketed)
            { 
                if (__instance.IsOwner && HUDManager.Instance.holdFillAmount <= 0f && __instance.playerHeldBy.cursorTip.text == "" && __instance.shellsLoaded > 0)
                {
                    if (IngamePlayerSettings.Instance.playerInput.actions.FindAction("ItemTertiaryUse").IsPressed())
                    {
                        if (Time.realtimeSinceStartup - startTime > 1f)// this is all the usual interaction calls, meaning vanilla logic should take over from here
                        {
                            holdingDown = false;
                            __instance.ItemInteractLeftRight(true);
                            __instance.isSendingItemRPC++;
                            __instance.InteractLeftRightServerRpc(true);
                        }
                    }
                    else
                    {
                        //ScienceBirdTweaks.Logger.LogDebug($"STOPPING HOLD");
                        holdingDown = false;
                    }
                }
                else
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"STOPPING HOLD {__instance.IsOwner}, {__instance.isHeld}, {HUDManager.Instance.holdFillAmount <= 0f}, {__instance.playerHeldBy.cursorTip.text == ""}, {__instance.shellsLoaded > 0} ");
                    holdingDown = false;
                }
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ItemInteractLeftRight))]
        [HarmonyPostfix]
        static void ReloadInteract(ShotgunItem __instance, bool right)// this method is only reached if the local client gets through the above functions
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || __instance.isHeldByEnemy) { return; }

            if (unloadEnabled && right && (__instance.FindAmmoInInventory() == -1 || __instance.shellsLoaded >= 2) && __instance.shellsLoaded > 0)
            {
                if (!shellRegistered)
                {
                    if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(shellPrefab))
                    {
                        NutcrackerEnemyAI nutcracker = Resources.FindObjectsOfTypeAll<NutcrackerEnemyAI>().First();
                        if (nutcracker != null)
                        {
                            shellPrefab = nutcracker.shotgunShellPrefab;
                            ScienceBirdTweaks.Logger.LogDebug("Re-finding shell prefab on interact...");
                            if (shellPrefab.GetComponent<NetworkObject>().PrefabIdHash == 0)
                            {
                                ScienceBirdTweaks.Logger.LogError("Shell not registered on interaction due to an incompatibility. To avoid client de-syncs, enable the 'force register shells' config option. Please report this issue!");
                            }
                        }
                    }
                    else
                    {
                        shellRegistered = true;
                    }
                }

                ScienceBirdTweaks.Logger.LogDebug("Eject called!");

                if (__instance.IsServer)
                {
                    // in short: put it in the ship if it's in orbit, otherwise put it in the current round's scrap container
                    Transform parent = ((((!(__instance.playerHeldBy != null) || !__instance.playerHeldBy.isInElevator) && !StartOfRound.Instance.inShipPhase) || !(RoundManager.Instance.spawnedScrapContainer != null)) ? StartOfRound.Instance.elevatorTransform : RoundManager.Instance.spawnedScrapContainer);
                    for (int i = 0; i < __instance.shellsLoaded; i++)
                    {
                        GameObject obj = Object.Instantiate(shellPrefab, __instance.gameObject.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), -0.1f, Random.Range(-0.5f, 0.5f)), Quaternion.identity, parent);
                        obj.GetComponent<NetworkObject>().Spawn();
                        GrabbableObject grabbable = obj.GetComponent<NetworkObject>().GetComponent<GrabbableObject>();
                        grabbable.startFallingPosition = obj.transform.position;
                        grabbable.fallTime = 0f;
                        grabbable.hasHitGround = false;
                        grabbable.reachedFloorTarget = false;
                        if (__instance.playerHeldBy != null && __instance.playerHeldBy.isInHangarShipRoom)
                        {
                            __instance.playerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, grabbable);
                        }
                        else
                        {
                            grabbable.isInFactory = __instance.gameObject.GetComponent<GrabbableObject>().isInFactory;
                        }
                    }
                }
                __instance.shellsLoaded = 0;
                TooltipUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyPostfix]
        static void ShellRotationPatch(GrabbableObject __instance)// randomly rotate spawned shells
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            
            if (__instance.gameObject.GetComponent<GunAmmo>())
            {
                __instance.floorYRot = Random.Range(0, 360);
            }
            
        }
    }
}
