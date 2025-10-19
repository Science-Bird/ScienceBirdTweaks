using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using ScienceBirdTweaks.Patches;

namespace ScienceBirdTweaks.ZapGun
{
    [HarmonyPatch]
    public class HazardPatches
    {
        public static GameObject doorPrefab;
        public static Color spikesGreen = new Color(0.3254902f, 1f, 0.3679014f, 1f);
        public static AudioClip disabledSFX;
        public static Material disabledMat;
        public static Material offMat;
        public static RuntimeAnimatorController newController;
        public static bool extraTrigger = false;

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        public static void InitializeAssets()
        {
            if (!ScienceBirdTweaks.SpikeTrapDisableAnimation.Value && !ScienceBirdTweaks.MineDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            doorPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("DoorKillTrigger");
            disabledMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SpikeRoofTrapDisabledMat");
            offMat = (Material)ScienceBirdTweaks.TweaksAssets.LoadAsset("SpikeRoofTrapOffMat");
            disabledSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("SingleClick");
            switch (ScienceBirdTweaks.MineSoundEffect.Value)
            {
                case "Beep":
                    disabledSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("SingleBeep");
                    break;
                case "None":
                    disabledSFX = (AudioClip)ScienceBirdTweaks.TweaksAssets.LoadAsset("SingleSilence");
                    break;
            }
            newController = (RuntimeAnimatorController)ScienceBirdTweaks.TweaksAssets.LoadAsset("landmineAltController");
        }

        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.Start))]
        [HarmonyPostfix]
        static void BigDoorsPatch(TerminalAccessibleObject __instance)
        {
            if (!__instance.isBigDoor || (!ScienceBirdTweaks.PlayerLethalBigDoors.Value && !ScienceBirdTweaks.EnemyLethalBigDoors.Value && !ScienceBirdTweaks.ZappableBigDoors.Value) || !ScienceBirdTweaks.ZapGunRework.Value)
            {
                return;
            }
            InitializeBigDoors(__instance);
            __instance.gameObject.AddComponent<DoorZapper>();
        }

        public static void InitializeBigDoors(TerminalAccessibleObject terminalObj)
        {
            if ((ScienceBirdTweaks.PlayerLethalBigDoors.Value || ScienceBirdTweaks.EnemyLethalBigDoors.Value) && !terminalObj.gameObject.transform.Find("DoorKillTrigger(Clone)"))
            {
                GameObject doorObj = Object.Instantiate(doorPrefab, Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
                doorObj.transform.SetParent(terminalObj.gameObject.transform, false);
                GameObject doorTrigger = doorObj.transform.Find("Trigger").gameObject;
                doorTrigger.transform.localPosition = new Vector3(0f, 2f, -2.623f);
                doorTrigger.AddComponent<KillOnStay>();
            }
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
            if (ScienceBirdTweaks.SpikesCooldownMute.Value)
            {
                if (__instance.transform.parent != null && __instance.transform.parent.parent != null && __instance.transform.parent.parent.parent != null)
                {
                    Transform superCreak = __instance.transform.parent.parent.parent.Find("CreakingSFX");
                    if (superCreak != null)
                    {
                        superCreak.gameObject.GetComponent<AudioSource>().mute = !enabled;
                    }
                }
            }

            if (!ScienceBirdTweaks.SpikeTrapDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            GameObject animObj = __instance.gameObject.transform.parent.gameObject;
            SpikesZapper zapper = animObj.GetComponentInChildren<SpikesZapper>();
            if (zapper != null && !zapper.tempStun)
            {
                if (!enabled)
                {
                    if (BlackoutTriggerPatches.doingHazardShutdown)
                    {
                        zapper.light.intensity = 0f;
                        Material[] materials = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                        materials[0] = offMat;
                        zapper.supportLights.GetComponent<MeshRenderer>().materials = materials;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Starting spike trap special animation!");
                        zapper.light.intensity = 2f;
                        zapper.light.colorTemperature = 6580f;
                        zapper.light.color = spikesGreen;
                        Material[] mats = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                        mats[0] = disabledMat;
                        zapper.supportLights.GetComponent<MeshRenderer>().materials = mats;
                        zapper.startRoutine = true;
                    }
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogDebug($"Ending spike trap special animation!");
                    if (zapper == null) { return; }
                    zapper.light.intensity = 1.172347f;
                    zapper.light.colorTemperature = 1500f;
                    zapper.light.color = Color.white;
                    Material[] mats = zapper.supportLights.GetComponent<MeshRenderer>().materials;
                    mats[0] = zapper.originalMat;
                    zapper.supportLights.GetComponent<MeshRenderer>().materials = mats;
                }
            }
        }


        [HarmonyPatch(typeof(Landmine), nameof(Landmine.TriggerMineOnLocalClientByExiting))]
        [HarmonyPrefix]
        public static bool DetonateCheck1(Landmine __instance)// make sure mine animator doesnt go off unintentionally
        {
            if (!ScienceBirdTweaks.MineDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return true; }

            if (!__instance.mineActivated || (__instance.GetComponent<MineZapper>() && (__instance.GetComponent<MineZapper>().tempStun || __instance.GetComponent<MineZapper>().light1.intensity == 0f)))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.SetOffMineAnimation))]
        [HarmonyPrefix]
        public static bool DetonateCheck2(Landmine __instance)// make sure mine animator doesnt go off unintentionally (the sequel)
        {
            if (!ScienceBirdTweaks.MineDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return true; }

            if (!__instance.mineActivated || (__instance.GetComponent<MineZapper>() && (__instance.GetComponent<MineZapper>().tempStun || __instance.GetComponent<MineZapper>().light1.intensity == 0f)))
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(Landmine), nameof(Landmine.OnTriggerExit))]
        [HarmonyPrefix]
        public static void MineTriggerExitFix(Landmine __instance, Collider other)
        {
            if (!ScienceBirdTweaks.LandmineFix.Value) { return; }

            if (!__instance.mineActivated && other.CompareTag("Player"))
            {
                PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                if (component != null && !component.isPlayerDead && !(component != GameNetworkManager.Instance.localPlayerController))
                {
                    __instance.localPlayerOnMine = false;
                }
            }
        }

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.ToggleMineEnabledLocalClient))]
        [HarmonyPostfix]
        public static void MineCooldownPatch(Landmine __instance, bool enabled)
        {
            if (!ScienceBirdTweaks.MineDisableAnimation.Value && !ScienceBirdTweaks.ZapGunRework.Value) { return; }

            MineZapper zapper = __instance.GetComponent<MineZapper>();
            if (zapper != null && !zapper.tempStun && !zapper.disabled)
            {
                if (!enabled)
                {
                    if (BlackoutTriggerPatches.doingHazardShutdown)
                    {
                        __instance.mineAudio.Stop();
                        __instance.mineAudio.volume = 0f;
                        zapper.light1.intensity = 0f;
                        zapper.light2.intensity = 0f;
                        zapper.indirectLight.intensity = 0f;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Starting landmine special animation!");
                        __instance.mineAnimator.SetBool("disabled", true);
                        zapper.light1.intensity = 227.6638f;
                        zapper.light2.intensity = 227.6638f;
                        zapper.indirectLight.intensity = 436.6049f;
                        zapper.startRoutine = true;
                        extraTrigger = true;
                    }
                }
                else
                {
                    if (BlackoutTriggerPatches.doingHazardStartup)
                    {
                        __instance.mineAudio.Play();
                        __instance.mineAudio.volume = 1f;
                        zapper.light1.intensity = 227.6638f;
                        zapper.light2.intensity = 227.6638f;
                        zapper.indirectLight.intensity = 436.6049f;
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Ending landmine special animation!");
                        __instance.mineAnimator.SetBool("disabled", false);
                    }
                }
            }
        }
    }
}
