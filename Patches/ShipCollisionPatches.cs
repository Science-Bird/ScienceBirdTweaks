using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ShipCollisionPatches
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ReplaceRailingCollision(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.ConsistentCatwalkCollision.Value)
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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ExtendLeverCollision(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.LargerLeverCollision.Value)
            {
                return;
            }
            StartMatchLever lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
            if (lever != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Extending lever collider...");
                BoxCollider leverCollider = lever.gameObject.GetComponent<BoxCollider>();
                if (leverCollider != null)
                {
                    leverCollider.size = ScienceBirdTweaks.ConfigLeverSize;
                }

            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void DestroyBottomCollision(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.BegoneBottomCollision.Value)
            {
                return;
            }
            if (sceneName != "SampleSceneRelay" && sceneName != "MainMenu")
            {
                GameObject bottom = GameObject.Find("Environment/HangarShip/ShipBottomColliders");
                if (bottom != null)
                {
                    ScienceBirdTweaks.Logger.LogDebug("Destroying bottom colliders...");
                    BoxCollider[] colliders = bottom.GetComponentsInChildren<BoxCollider>();
                    foreach (BoxCollider collider in colliders)
                    {
                        collider.enabled = false;
                    }
                }
                GameObject cube = GameObject.Find("Environment/HangarShip/Cube");
                if (cube != null)
                {
                    BoxCollider cubeCollider = cube.GetComponent<BoxCollider>();
                    if (cubeCollider != null)
                    {
                        cubeCollider.enabled = false;
                    }
                }
            }
        }

        static void TeleporterBuildCollision()
        {
            GameObject teleportObj = GameObject.Find("/Teleporter(Clone)/AnimContainer/PlacementCollider");
            GameObject inverseTeleportObj = GameObject.Find("/InverseTeleporter(Clone)/AnimContainer/PlacementCollider");
            if (teleportObj != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Fixing teleporter build collision...");
                BoxCollider teleporterPlace = teleportObj.GetComponent<BoxCollider>();
                if (teleporterPlace != null)
                {
                    teleporterPlace.size = ScienceBirdTweaks.ConfigTeleporterSize;
                }
            }
            if (inverseTeleportObj != null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Fixing inverse teleporter build collision...");
                BoxCollider teleporterPlace = inverseTeleportObj.GetComponent<BoxCollider>();
                if (teleporterPlace != null)
                {
                    teleporterPlace.size = ScienceBirdTweaks.ConfigTeleporterSize;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void TeleporterBuildOnLoad(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                TeleporterBuildCollision();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableClientRpc))]
        [HarmonyPostfix]
        static void TeleporterBuildOnBuy(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                TeleporterBuildCollision();
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        [HarmonyPostfix]
        static void TeleporterBuildOnSpawn(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                TeleporterBuildCollision();
            }
        }
    }
}
