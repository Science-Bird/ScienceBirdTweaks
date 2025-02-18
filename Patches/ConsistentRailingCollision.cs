using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ConsistentRailingCollision
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ReplaceRailingCollision(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.ConsistentRailingCollision.Value)
            {
                return;
            }
            if (sceneName != "SampleSceneRelay" && sceneName != "MainMenu")
            {
                GameObject catwalk = GameObject.Find("CatwalkShip");
                if (catwalk != null)
                {
                    GameObject newCatwalk = (GameObject)ScienceBirdTweaks.TweaksAssets.LoadAsset("CatwalkShipAltered");
                    MeshFilter newCatwalkMesh = newCatwalk.GetComponentInChildren<MeshFilter>();
                    MeshCollider catwalkCollider = catwalk.GetComponent<MeshCollider>();
                    if (newCatwalkMesh != null)
                    {
                        catwalkCollider.sharedMesh = newCatwalkMesh.sharedMesh;
                    }
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogError("Couldn't find catwalk!");
                }
            }
        }
    }
}
