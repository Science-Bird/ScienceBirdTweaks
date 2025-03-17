using HarmonyLib;
using UnityEngine;
using WesleyMoonScripts.Components;
using Unity.Netcode;
using UnityEngine.Video;
using GameNetcodeStuff;

namespace ScienceBirdTweaks.ModPatches
{
    public class WesleyPatches
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(typeof(LevelCassetteLoader).GetMethod(nameof(LevelCassetteLoader.LoadCassette)), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("OnLoadTape")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "TapeEnded"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("OnTapeEnd")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "Update"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("TapeUpdate")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(GameNetworkManager), "Start"), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("InitializeInteractPrefab")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "StartLoadingCassette"), prefix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("StartLoad")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(LevelCassetteLoader), "LoadCassetteClientRpc"), prefix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("LoadClient")), postfix: new HarmonyMethod(typeof(TapeSkipPatches).GetMethod("LoadClientAfter")));
        }
    }

    public class TapeSkipPatches
    {
        public static LevelCassetteLoader currentLoader;
        public static GameObject interactPrefab;
        public static bool adjustTransform = false;
        public static ulong bufferID = 115;
        public static ulong accessID = 115;
        public static bool isPlayerSending;

        public static void InitializeInteractPrefab(GameNetworkManager __instance)
        {
            if (!ScienceBirdTweaks.VideoTapeSkip.Value) { return; }
            ScienceBirdTweaks.Logger.LogDebug("Initializing interact object!");
            interactPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("SkipInteract");
            NetworkManager.Singleton.AddNetworkPrefab(interactPrefab);
        }

        public static void TapeUpdate(LevelCassetteLoader __instance)
        {
            if (!ScienceBirdTweaks.VideoTapeSkip.Value) { return; }
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
            if (!ScienceBirdTweaks.VideoTapeSkip.Value) { return; }
            if (__instance.screenPlayer == null)
            {
                return;
            }
            currentLoader = __instance;
            adjustTransform = false;
            if (__instance.IsServer)
            {
                GameObject skipInteract = Object.Instantiate(interactPrefab, Vector3.zero, Quaternion.identity);
                skipInteract.GetComponent<NetworkObject>().Spawn();
            }
            adjustTransform = true;
        }

        public static void OnTapeEnd(LevelCassetteLoader __instance)
        {
            if (!ScienceBirdTweaks.VideoTapeSkip.Value) { return; }
            if (__instance.screenPlayer == null)
            {
                return;
            }
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
                    Object.Destroy(skipInteract);
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogWarning("Tape ended with network object not spawned!");
                }
            }
        }


        public static void StartLoad(LevelCassetteLoader __instance, PlayerControllerB player)
        {
            if (!ScienceBirdTweaks.VideoTapeInsertFix.Value) { return; }
            
                ScienceBirdTweaks.Logger.LogDebug($"Log cycle 1:");
            foreach (PlayerControllerB playerScript in RoundManager.Instance.playersManager.allPlayerScripts)
            {
                ScienceBirdTweaks.Logger.LogDebug($"playerId: {playerScript.playerClientId}, actualId: {playerScript.actualClientId}, heldObject: {playerScript.currentlyHeldObjectServer}");
            }
            ScienceBirdTweaks.Logger.LogDebug($"ME; playerId: {player.playerClientId}, actualId: {player.actualClientId}, heldObject: {player.currentlyHeldObjectServer}");
            if (player.actualClientId != player.playerClientId)
            {
                bufferID = player.actualClientId;
                accessID = player.playerClientId;
                player.actualClientId = player.playerClientId;
                ScienceBirdTweaks.Logger.LogDebug($"ME UPDATED; playerId: {player.playerClientId}, actualId: {player.actualClientId}, heldObject: {player.currentlyHeldObjectServer}");
            }
        }

        public static void LoadClient(LevelCassetteLoader __instance, ref int playerWhoSent)
        {
            if (!ScienceBirdTweaks.VideoTapeInsertFix.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug($"Log cycle 2:");
            foreach (PlayerControllerB playerScript in RoundManager.Instance.playersManager.allPlayerScripts)
            {
                ScienceBirdTweaks.Logger.LogDebug($"playerId: {playerScript.playerClientId}, actualId: {playerScript.actualClientId}, heldObject: {playerScript.currentlyHeldObjectServer}");
            }
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            ScienceBirdTweaks.Logger.LogDebug($"ME; playerId: {player.playerClientId}, actualId: {player.actualClientId}, heldObject: {player.currentlyHeldObjectServer}");
        }

        public static void LoadClientAfter(LevelCassetteLoader __instance, ref int playerWhoSent)
        {
            if (!ScienceBirdTweaks.VideoTapeInsertFix.Value) { return; }

            ScienceBirdTweaks.Logger.LogDebug($"Log cycle 3:");
            if (bufferID != 115 && accessID != 115)
            {
                RoundManager.Instance.playersManager.allPlayerScripts[accessID].actualClientId = bufferID;
                bufferID = 115;
                accessID = 115;
            }
            foreach (PlayerControllerB playerScript in RoundManager.Instance.playersManager.allPlayerScripts)
            {
                ScienceBirdTweaks.Logger.LogDebug($"playerId: {playerScript.playerClientId}, actualId: {playerScript.actualClientId}, heldObject: {playerScript.currentlyHeldObjectServer}");
            }
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            ScienceBirdTweaks.Logger.LogDebug($"ME; playerId: {player.playerClientId}, actualId: {player.actualClientId}, heldObject: {player.currentlyHeldObjectServer}");
        }
    }
}
