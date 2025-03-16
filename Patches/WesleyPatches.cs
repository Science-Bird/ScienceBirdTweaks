using HarmonyLib;
using UnityEngine;
using WesleyMoonScripts.Components;
using Unity.Netcode;
using UnityEngine.Video;
using GameNetcodeStuff;

namespace ScienceBirdTweaks.Patches
{
    public class WesleyPatches
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(typeof(LevelCassetteLoader).GetMethod(nameof(LevelCassetteLoader.LoadCassette)), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("OnLoadTape")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "TapeEnded"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("OnTapeEnd")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "Update"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("TapeUpdate")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(GameNetworkManager), "Start"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("InitializeInteractPrefab")));
        }
    }

    public class TapeSkipPatches
    {
        public static LevelCassetteLoader currentLoader;
        public static GameObject interactPrefab;
        public static bool adjustTransform = false;

        public static void InitializeInteractPrefab(LevelCassetteLoader __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Initializing interact object!");
            interactPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("SkipInteract");
            NetworkManager.Singleton.AddNetworkPrefab(interactPrefab);
        }

        public static void TapeUpdate(LevelCassetteLoader __instance)
        {
            if (__instance.isTapePlaying && adjustTransform)
            {
                GameObject skipInteractObj = GameObject.Find("SkipInteract(Clone)");
                InteractTrigger tapeInteract = __instance.gameObject.GetComponentInChildren<InteractTrigger>();
                if (skipInteractObj != null && tapeInteract != null)
                {
                    skipInteractObj.transform.position = tapeInteract.transform.position;
                    skipInteractObj.transform.rotation = tapeInteract.transform.rotation;
                    skipInteractObj.transform.localScale = tapeInteract.transform.localScale;
                    BoxCollider collider = skipInteractObj.GetComponent<BoxCollider>();
                    collider.size = tapeInteract.gameObject.GetComponent<BoxCollider>().size;
                    adjustTransform = false;
                }
            }
        }

        public static void OnLoadTape(LevelCassetteLoader __instance)
        {
            currentLoader = __instance;
            adjustTransform = false;
            if (__instance.IsServer)
            {
                GameObject skipInteract = UnityEngine.Object.Instantiate(interactPrefab, Vector3.zero, Quaternion.identity);
                skipInteract.GetComponent<NetworkObject>().Spawn();
            }
            adjustTransform = true;
        }

        public static void OnTapeEnd(LevelCassetteLoader __instance)
        {
            __instance.screenPlayer.Stop();
            __instance.screenPlayer.clip = __instance.awakeClip;
            __instance.screenPlayer.Play();
            if (__instance.audioPlayer != null)
            {
                __instance.screenPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                __instance.screenPlayer.EnableAudioTrack(0, true);
                __instance.screenPlayer.SetTargetAudioSource(0, __instance.audioPlayer);
                __instance.screenPlayer.controlledAudioTrackCount = 1;
            }
            GameObject skipInteract = GameObject.Find("SkipInteract(Clone)");
            if (skipInteract != null && __instance.IsServer)
            {
                if (skipInteract.GetComponent<NetworkObject>().IsSpawned)
                {
                    skipInteract.GetComponent<NetworkObject>().Despawn();
                    UnityEngine.Object.Destroy(skipInteract);
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogWarning("Tape ended with network object not spawned!");
                }

            }
        }
    }
}
