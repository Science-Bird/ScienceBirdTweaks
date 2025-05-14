using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using Steamworks.Ugc;
using Unity.Netcode;
using UnityEngine;
using ScienceBirdTweaks.Scripts;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ShotgunTooltipPatches
    {
        public static string safetyOnText;
        public static string safetyOffText;
        public static string reloadString;
        public static string ammoString0;
        public static string ammoString1;
        public static string ammoString2;
        public static string ejectCheckString;
        public static bool showAmmo = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            Item shotgun = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.itemName == "Shotgun").First();
            if (shotgun != null)
            {
                if (shotgun.toolTips != null && shotgun.toolTips.Length == 3)
                {
                    safetyOnText = ScienceBirdTweaks.SafetyOnString.Value + " " + shotgun.toolTips[2].Remove(0, shotgun.toolTips[2].IndexOf(":"));
                    safetyOffText = ScienceBirdTweaks.SafetyOffString.Value + " " + shotgun.toolTips[2].Remove(0, shotgun.toolTips[2].IndexOf(":"));
                    reloadString = "Reload" + " " + shotgun.toolTips[1].Remove(0, shotgun.toolTips[1].IndexOf(":"));
                    ammoString0 = "( )( ) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                    ammoString1 = "(O)( ) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                    ammoString2 = "(O)(O) Fire" + " " + shotgun.toolTips[0].Remove(0, shotgun.toolTips[0].IndexOf(":"));
                    shotgun.toolTips[2] = safetyOnText;
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
            showAmmo = ScienceBirdTweaks.ShowAmmo.Value;
            ejectCheckString = ScienceBirdTweaks.DoAmmoCheck.Value ? "Open chambers : [E]" : "Eject shells : [Hold E]";
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
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || !ShotgunPatches.HasValidHolder(__instance)) { return; }

            string changeTo = ((!__instance.safetyOn) ? safetyOffText : safetyOnText);
            if (__instance.IsOwner)
            {
                ScienceBirdTweaks.Logger.LogDebug($"Safety: {__instance.safetyOn}, setting text to {changeTo}");
                int num = 3;
                if (ShotgunPatches.unloadEnabled && __instance.shellsLoaded <= 0 && !ShotgunPatches.ammoCheck && !ShotgunPatches.AllowedToEject(__instance))
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
            TooltipUpdate(shotgun);
        }

        public static void TooltipUpdate(ShotgunItem shotgun)
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || !ShotgunPatches.HasValidHolder(shotgun, false)) { return; }

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
                if (ShotgunPatches.unloadEnabled || ShotgunPatches.ammoCheck)
                {
                    if (ShotgunPatches.AmmoInInventory(shotgun) && shotgun.shellsLoaded < 2)
                    {
                        toolTips[1] = reloadString;
                    }
                    else if (shotgun.shellsLoaded > 0 || ShotgunPatches.ammoCheck)
                    {
                        toolTips[1] = ejectCheckString;
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

    }

    [HarmonyPatch]
    public class ShotgunPatches
    {
        public static GameObject shellPrefab;
        public static AudioClip inspectSFX;
        public static bool unloadEnabled = false;
        public static bool ammoCheck = false;
        public static float startTime;
        public static bool shellRegistered = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            unloadEnabled = ScienceBirdTweaks.UnloadShells.Value;
            ammoCheck = ScienceBirdTweaks.DoAmmoCheck.Value;

            GrabbableObject[] shotguns = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is ShotgunItem).ToArray();
            foreach (GrabbableObject shotgun in shotguns)
            {
                if (ScienceBirdTweaks.PickUpGunOrbit.Value)
                {
                    shotgun.itemProperties.canBeGrabbedBeforeGameStart = true;
                }
                if (unloadEnabled || ammoCheck)
                {
                    inspectSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("ShotgunInspectAudio");
                    shotgun.gameObject.AddComponent<ShotgunScript>();
                }
            }

            if (unloadEnabled || ScienceBirdTweaks.PickUpShellsOrbit.Value)
            {
                GrabbableObject[] shells = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is GunAmmo).ToArray();
                foreach (GrabbableObject shell in shells)
                {
                    if (ScienceBirdTweaks.PickUpShellsOrbit.Value)
                    {
                        shell.itemProperties.canBeGrabbedBeforeGameStart = true;
                    }
                    if (unloadEnabled && shell.gameObject.GetComponent<NetworkObject>() != null)
                    {
                        if (shell.gameObject.GetComponent<NetworkObject>().PrefabIdHash == 0)
                        {
                            ScienceBirdTweaks.Logger.LogDebug("No shell found on initialization!");
                            shellRegistered = false;
                            //if (ScienceBirdTweaks.ForceRegisterShells.Value)
                            //{
                            //    shellPrefab = shell.gameObject;
                            //    ScienceBirdTweaks.Logger.LogInfo("Manually registering shell prefabs with network manager!");
                            //    NetworkManager.Singleton.AddNetworkPrefab(shellPrefab);
                            //    if (shellPrefab.GetComponent<NetworkObject>().PrefabIdHash != 0)
                            //    {
                            //        shellRegistered = true;
                            //        break;
                            //    }
                            //}
                        }
                        else
                        {
                            shellRegistered = true;
                            ScienceBirdTweaks.Logger.LogDebug("Found shell!");
                            shellPrefab = shell.gameObject;
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ShellPrefabCheck(StartOfRound __instance, string sceneName)// some mods cause the network registration of this prefab to be delayed, so it's double-checked here
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.ShotgunMasterDisable.Value || !unloadEnabled) { return; }

            if (sceneName == "SampleSceneRelay")
            {
                if (shellPrefab == null || !NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(shellPrefab))
                {
                    GrabbableObject[] shells = Resources.FindObjectsOfTypeAll<GrabbableObject>().Where(x => x is GunAmmo).ToArray();
                    foreach (GrabbableObject shell in shells)
                    {
                        if (shell.gameObject.GetComponent<NetworkObject>() != null)
                        {
                            if (shell.gameObject.GetComponent<NetworkObject>().PrefabIdHash == 0)
                            {
                                ScienceBirdTweaks.Logger.LogDebug("No shell found on load!");
                                shellRegistered = false;
                            }
                            else
                            {
                                ScienceBirdTweaks.Logger.LogDebug("Found shell!");
                                shellRegistered = true;
                                shellPrefab = shell.gameObject;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static bool AmmoInInventory(ShotgunItem shotgun)
        {
            for (int i = 0; i < shotgun.playerHeldBy.ItemSlots.Length; i++)
            {
                if (!(shotgun.playerHeldBy.ItemSlots[i] == null))
                {
                    GunAmmo gunAmmo = shotgun.playerHeldBy.ItemSlots[i] as GunAmmo;
                    if (gunAmmo != null && gunAmmo.ammoType == shotgun.gunCompatibleAmmoID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool AllowedToEject(ShotgunItem shotgun)
        {
            return unloadEnabled && (!AmmoInInventory(shotgun) || shotgun.shellsLoaded >= 2) && shotgun.shellsLoaded > 0;
        }

        public static bool HasValidHolder(ShotgunItem shotgun, bool strict = true)
        {
            return shotgun != null && shotgun.playerHeldBy != null && (!strict || (shotgun.isHeld && !shotgun.isPocketed)) && !shotgun.isHeldByEnemy;
        }

        public static bool LocalPlayerNotInteracting(ShotgunItem shotgun)
        {
            return shotgun.IsOwner && HUDManager.Instance.holdFillAmount <= 0f && shotgun.playerHeldBy.cursorTip.text == "";
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemInteractLeftRightOnClient))]
        [HarmonyPrefix]
        static void InteractPrefix(GrabbableObject __instance, bool right)// this exists to interrupt the usual interaction event if the eject requirements are met, this is so the eject procedure can do some client-side checks and do the hold event before starting synced interaction with other clients
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.ShotgunMasterDisable.Value && (unloadEnabled || ammoCheck) && __instance.GetComponent<ShotgunItem>() && right && !__instance.GetComponent<ShotgunItem>().isReloading && (!AmmoInInventory(__instance.GetComponent<ShotgunItem>()) || __instance.GetComponent<ShotgunItem>().shellsLoaded >= 2))
            {
                ShotgunItem shotgun = __instance.GetComponent<ShotgunItem>();
                bool ejecting = AllowedToEject(shotgun);
                if (LocalPlayerNotInteracting(shotgun))// make sure player isn't doing some other kind of ongoing interaction
                {
                    if (__instance.GetComponent<ShotgunScript>())
                    {
                        __instance.GetComponent<ShotgunScript>().StartHolding(__instance.GetComponent<ShotgunItem>(), ejecting, ammoCheck);
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogError("No shotgun script found!");
                    }
                }
                else
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"ABORTING HOLD {__instance.IsOwner}, {__instance.isHeld}, {HUDManager.Instance.holdFillAmount <= 0f}, {__instance.playerHeldBy.cursorTip.text == ""}");
                }
            }
            else
            {
                //ScienceBirdTweaks.Logger.LogDebug("EARLY ABORT");
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyPostfix]
        static void ShellRotationPatch(GrabbableObject __instance)// randomly rotate spawned shells
        {
            if (ScienceBirdTweaks.ClientsideMode.Value || ScienceBirdTweaks.ShotgunMasterDisable.Value) { return; }

            if (__instance.gameObject.GetComponent<GunAmmo>())
            {
                __instance.floorYRot = Random.Range(0, 360);
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.StopUsingGun))]
        [HarmonyPostfix]
        public static void StopUsingGunPrefix(ShotgunItem __instance)
        {
            if (!ScienceBirdTweaks.ClientsideMode.Value && !ScienceBirdTweaks.ShotgunMasterDisable.Value && (ammoCheck || unloadEnabled))
            {
                PlayerControllerB player = __instance.playerHeldBy ?? __instance.previousPlayerHeldBy;
                if (ammoCheck)
                {
                    player.playerBodyAnimator.speed = 1f;
                    __instance.gunAudio.Stop();
                    __instance.gunAnimator.SetBool("Reloading", value: false);
                    player.playerBodyAnimator.SetBool("ReloadShotgun", false);
                }
                __instance.isReloading = false;
                
                if (__instance.gameObject.GetComponent<ShotgunScript>())
                {
                    __instance.gameObject.GetComponent<ShotgunScript>().fill = 0f;
                    __instance.gameObject.GetComponent<ShotgunScript>().StopAllCoroutines();
                }
            }
        }
    }
}
