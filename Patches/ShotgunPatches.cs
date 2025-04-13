using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
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
        public static bool unloadEnabled = false;
        public static bool showAmmo = true;
        public static bool holdingDown = false;
        public static float startTime;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            safetyOnText = ScienceBirdTweaks.SafetyOnString.Value;
            safetyOffText = ScienceBirdTweaks.SafetyOffString.Value;

            if (ScienceBirdTweaks.PickUpGunOrbit.Value)
            {
                Item shotgun = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Shotgun").First();
                if (shotgun != null)
                {
                    shotgun.canBeGrabbedBeforeGameStart = true;
                }
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
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ShellPrefabCheck(StartOfRound __instance)// some mods cause the network registration of this prefab to be delayed, so it's double-checked here
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(shellPrefab))
            {
                NutcrackerEnemyAI nutcracker = Resources.FindObjectsOfTypeAll<NutcrackerEnemyAI>().First();
                if (nutcracker != null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Re-finding shell prefab!");
                    shellPrefab = nutcracker.shotgunShellPrefab;
                }
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.SetControlTipsForItem))]
        [HarmonyPostfix]
        public static void TooltipSet(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.SetSafetyControlTip))]
        [HarmonyPostfix]
        public static void SafetySet(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
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
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            ScienceBirdTweaks.Logger.LogDebug("Reload ended, updating tooltip!");
            TooltipUpdate(__instance);
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
        [HarmonyPostfix]
        public static void PostShot(ShotgunItem __instance)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
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
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }
            if (shotgun.IsOwner)
            {
                string[] toolTips = shotgun.itemProperties.toolTips;
                if (showAmmo)
                {
                    switch (shotgun.shellsLoaded)
                    {
                        case 0:
                            toolTips[0] = "( )( ) Fire : [LMB]";
                            break;
                        case 1:
                            toolTips[0] = "(O)( ) Fire : [LMB]";
                            break;
                        case 2:
                            toolTips[0] = "(O)(O) Fire : [LMB]";
                            break;
                        default:
                            toolTips[0] = "Fire : [LMB]";
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
                        toolTips[1] = "Reload : [E]";
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
                if (__instance.IsOwner && __instance.isHeld && HUDManager.Instance.holdFillAmount <= 0f && __instance.playerHeldBy.cursorTip.text == "")// make sure player isn't doing some other kind of ongoing interaction
                {
                    LocalInteract(__instance.GetComponent<ShotgunItem>(), right);
                }
                // this inner failure will still return false (halting the interaction), because the basic eject criteria were met, we shouldn't try to do any interaction at all
                return false;
            }
            return true;
        }


        static void LocalInteract(ShotgunItem shotgun, bool right)// initialize local hold event
        {
            if (unloadEnabled && shotgun.IsOwner && shotgun.isHeld && HUDManager.Instance.holdFillAmount <= 0f && shotgun.playerHeldBy.cursorTip.text == "" && right && (shotgun.FindAmmoInInventory() == -1 || shotgun.shellsLoaded >= 2) && shotgun.shellsLoaded > 0)
            {
                holdingDown = true;
                startTime = Time.realtimeSinceStartup;
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void AttemptInteract(ShotgunItem __instance)// make sure button is held for 1 second
        {
            if (holdingDown)
            { 
                if (__instance.IsOwner && __instance.isHeld && HUDManager.Instance.holdFillAmount <= 0f && __instance.playerHeldBy.cursorTip.text == "" && __instance.shellsLoaded > 0)
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
                        holdingDown = false;
                    }
                }
                else
                {
                    holdingDown = false;
                }
            }
        }


        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ItemInteractLeftRight))]
        [HarmonyPostfix]
        static void ReloadInteract(ShotgunItem __instance, bool right)// this method is only reached if the local client gets through the above functions
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug(__instance.playerHeldBy.isHoldingInteract);
            if (unloadEnabled && right && (__instance.FindAmmoInInventory() == -1 || __instance.shellsLoaded >= 2) && __instance.shellsLoaded > 0)
            {
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
