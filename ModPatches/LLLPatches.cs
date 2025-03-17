using HarmonyLib;
using UnityEngine;
using Unity.Netcode;

namespace ScienceBirdTweaks.ModPatches
{
    public class LLLPatches
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(GameNetworkManager), "Start"), postfix: new HarmonyMethod(typeof(LLLSyncPatch).GetMethod("InitializeSyncPrefab")));
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc)), postfix: new HarmonyMethod(typeof(LLLSyncPatch).GetMethod("OnLateClientSync")));
        }
    }

    public class LLLSyncPatch
    {
        public static GameObject syncPrefab;

        public static void InitializeSyncPrefab(GameNetworkManager __instance)
        {
            ScienceBirdTweaks.Logger.LogDebug("Initializing sync object!");
            syncPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("LLLSyncScript");
            NetworkManager.Singleton.AddNetworkPrefab(syncPrefab);
        }

        public static void OnLateClientSync(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                GameObject syncObj = GameObject.Find("LLLSyncScript(Clone)");
                if (syncObj == null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Creating sync object since none exist...");
                    syncObj = Object.Instantiate(syncPrefab, Vector3.zero, Quaternion.identity);
                    syncObj.GetComponent<NetworkObject>().Spawn();
                }
                LLLUnlockSync syncScript = syncObj.GetComponent<LLLUnlockSync>();
                syncScript.CheckUnlocks();
            }
        }
    }
}
