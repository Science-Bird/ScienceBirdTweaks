using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks;

[HarmonyPatch]
public class NetworkObjectManager
{

    [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
    public static void Init()
    {
        if (networkPrefab != null || ScienceBirdTweaks.ClientsideMode.Value)
        {
            return;
        }

        networkPrefab = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("SBTweaksNetworkHandler");
        networkPrefab.AddComponent<NetworkHandler>();

        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    static void SpawnNetworkHandler()
    {
        if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && !ScienceBirdTweaks.ClientsideMode.Value)
        {
            var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn();
        }
    }

    static GameObject networkPrefab;
}