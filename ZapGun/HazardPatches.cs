using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections;
using DigitalRuby.ThunderAndLightning;
using Unity.Netcode;
using static UnityEngine.ParticleSystem.PlaybackState;
using ScienceBirdTweaks.Patches;

namespace ScienceBirdTweaks.ZapGun
{
    [HarmonyPatch]
    public class HazardPatches
    {
        public static GameObject doorPrefab;
        public static Color mineGreen = new Color(0.3254902f, 1f, 0.3679014f, 1f);
        public static Color mineGreenIndirect = new Color(0.6793682f, 1f, 0.6470588f, 1f);
        public static Color mineRed = new Color(1f, 0.3254717f, 0.3254717f, 1f);
        public static Color mineRedIndirect = new Color(1f, 0.6470588f, 0.6470588f, 1f);
        public static AudioClip disabledBeep;
        public static Material disabledMat;
        public static Material offMat;
        public static RuntimeAnimatorController newController;
        public static bool extraTrigger = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (!ScienceBirdTweaks.SpikeTrapDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            doorPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("DoorKillTrigger");
            disabledMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SpikeRoofTrapDisabledMat");
            offMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SpikeRoofTrapOffMat");
            disabledBeep = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("SingleBeep");
            newController = (RuntimeAnimatorController)ScienceBirdTweaks.TweaksAssets.LoadAsset("landmineAltController");
        }

        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.Start))]
        [HarmonyPostfix]
        static void BigDoorsPatch(TerminalAccessibleObject __instance)
        {
            if (!__instance.isBigDoor || (!ScienceBirdTweaks.PlayerLethalBigDoors.Value && !ScienceBirdTweaks.EnemyLethalBigDoors.Value) || !ScienceBirdTweaks.ZapGunRework.Value)
            {
                return;
            }
            if (!__instance.gameObject.transform.Find("DoorKillTrigger(Clone)"))
            {
                InitializeBigDoors(__instance);
            }
            __instance.gameObject.AddComponent<DoorZapper>();
        }

        public static void InitializeBigDoors(TerminalAccessibleObject terminalObj)
        {
            GameObject doorObj = Object.Instantiate(doorPrefab, Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
            doorObj.transform.SetParent(terminalObj.gameObject.transform, false);
            GameObject doorTrigger = doorObj.transform.Find("Trigger").gameObject;
            doorTrigger.transform.localPosition = new Vector3(0f, 2f, -2.623f);
            doorTrigger.AddComponent<KillOnStay>();
            Transform door1 = terminalObj.gameObject.transform.Find("BigDoorLeft");
            Transform door2 = terminalObj.gameObject.transform.Find("BigDoorRight");
            door1.gameObject.layer = 21;
            door2.gameObject.layer = 21;
        }

        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.SetDoorOpen))]
        [HarmonyPostfix]
        public static void DoorClosePatch(TerminalAccessibleObject __instance, bool open)
        {
            if (!__instance.isBigDoor || (!ScienceBirdTweaks.PlayerLethalBigDoors.Value && !ScienceBirdTweaks.EnemyLethalBigDoors.Value) || !ScienceBirdTweaks.ZapGunRework.Value)
            {
                return;
            }
            if (!__instance.gameObject.transform.Find("DoorKillTrigger(Clone)"))// for some reason the start method initialization wasn't running correctly, so this is a failsafe
            {
                InitializeBigDoors(__instance);
            }
            if (__instance.gameObject.transform.Find("DoorKillTrigger(Clone)").Find("Trigger"))// kill trigger has its own animation that moves with the door and disables the collider when not animating
            {
                GameObject doorObj = __instance.gameObject.transform.Find("DoorKillTrigger(Clone)").gameObject;
                GameObject trigger = doorObj.transform.Find("Trigger").gameObject;
                doorObj.GetComponent<Animator>().SetBool("open", open);
            }
        }


        [HarmonyPatch(typeof(SpikeRoofTrap), nameof(SpikeRoofTrap.ToggleSpikesEnabledLocalClient))]
        [HarmonyPostfix]
        public static void SpikeCooldownPatch(SpikeRoofTrap __instance, bool enabled)
        {
            if (!ScienceBirdTweaks.SpikeTrapDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug("SPIKE COOLDOWN");
            GameObject animObj = __instance.gameObject.transform.parent.gameObject;
            SpikesZapper zapper = animObj.GetComponentInChildren<SpikesZapper>();
            if (zapper != null && !zapper.tempStun)
            {
                if (!enabled)
                {
                    if (ApparatusRemovalPatch.doingHazardShutdown)
                    {
                        zapper.light.intensity = 0f;
                        Material[] materials = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                        materials[0] = offMat;
                        zapper.supportLights.GetComponent<MeshRenderer>().materials = materials;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"STARTING SPIKES SPECIAL ANIM");
                        zapper.light.intensity = 2f;
                        zapper.light.colorTemperature = 6580f;
                        zapper.light.color = mineGreen;
                        Material[] mats = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                        mats[0] = disabledMat;
                        zapper.supportLights.GetComponent<MeshRenderer>().materials = mats;
                        zapper.startRoutine = true;
                    }
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogDebug($"ENDING SPIKES SPECIAL ANIM");
                    if (zapper == null) { return; }
                    zapper.light.intensity = 1.172347f;
                    zapper.light.colorTemperature = 1500f;
                    zapper.light.color = Color.white;
                    Material[] mats = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                    ScienceBirdTweaks.Logger.LogDebug($"OG MAT: {zapper.originalMat.name}");
                    mats[0] = zapper.originalMat;
                    zapper.supportLights.GetComponent<MeshRenderer>().materials = mats;
                }
            }
            else if (zapper != null)
            {
                ScienceBirdTweaks.Logger.LogDebug($"SPIKES TEMPSTUN: {zapper.tempStun}");
            }
        }

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.ToggleMineEnabledLocalClient))]
        [HarmonyPostfix]
        public static void MineCooldownPatch(Landmine __instance, bool enabled)
        {
            if (!ScienceBirdTweaks.MineDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug("MINE COOLDOWN");
            MineZapper zapper = __instance.GetComponent<MineZapper>();
            if (zapper != null && !zapper.tempStun)
            {
                if (!enabled)
                {
                    if (ApparatusRemovalPatch.doingHazardShutdown)
                    {
                        __instance.mineAudio.Stop();
                        __instance.mineAudio.volume = 0f;
                        zapper.light1.intensity = 0f;
                        zapper.light2.intensity = 0f;
                        zapper.indirectLight.intensity = 0f;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"STARTING LANDMINE SPECIAL ANIM");
                        __instance.mineAnimator.SetBool("disabled", true);
                        ScienceBirdTweaks.Logger.LogDebug(__instance.mineAnimator.GetBoolString("disabled"));
                        //__instance.mineAudio.clip = disabledBeep;
                        //__instance.mineAudio.loop = true;
                        //__instance.mineAudio.Play();
                        zapper.light1.intensity = 227.6638f;
                        zapper.light2.intensity = 227.6638f;
                        zapper.indirectLight.intensity = 436.6049f;
                        //zapper.light1.colorTemperature = 6580f;
                        //zapper.light2.colorTemperature = 6580f;
                        //zapper.indirectLight.colorTemperature = 6580f;
                        //zapper.light1.color = mineGreen;
                        //zapper.light2.color = mineGreen;
                        //zapper.indirectLight.color = mineGreenIndirect;
                        zapper.startRoutine = true;
                        extraTrigger = true;
                    }
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogDebug($"ENDING LANDMINE SPECIAL ANIM");
                    __instance.mineAnimator.SetBool("disabled", false);
                    __instance.mineAudio.Stop();
                    __instance.mineAudio.loop = false;
                    __instance.mineAudio.clip = null;
                    zapper.light1.colorTemperature = 1500f;
                    zapper.light2.colorTemperature = 1500f;
                    zapper.indirectLight.colorTemperature = 1500f;
                    zapper.light1.color = mineRed;
                    zapper.light2.color = mineRed;
                    zapper.indirectLight.color = mineRedIndirect;
                    extraTrigger = false;
                }
            }
            else if (zapper != null)
            {
                ScienceBirdTweaks.Logger.LogDebug($"MINE TEMPSTUN: {zapper.tempStun}");
            }
        }

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.Update))]
        [HarmonyPostfix]
        public static void MineCooldownCheck(Landmine __instance)
        {
            if ((ScienceBirdTweaks.MineDisableAnimation.Value || ScienceBirdTweaks.ZapGunRework.Value) && !__instance.hasExploded && !__instance.sendingExplosionRPC)
            {
                if (extraTrigger)
                {
                    ScienceBirdTweaks.Logger.LogDebug($"{__instance.mineAnimator.GetBoolString("disabled")}, {__instance.mineAudio.isPlaying}, {__instance.mineAudio.clip}");
                    //__instance.mineAnimator.SetTrigger("disable");
                    //__instance.mineAudio.clip = disabledBeep;
                    //__instance.mineAudio.loop = true;
                    if (!__instance.mineAudio.isPlaying)
                    {
                        //__instance.mineAudio.Play();
                    }
                    //extraTrigger = false;
                }
            }
        }
    }
}
