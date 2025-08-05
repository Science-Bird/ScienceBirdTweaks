using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ShipCollisionPatches
    {
        public static bool doTeleporter = false;
        public static bool doInverse = false;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ReplaceRailingCollision(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.ConsistentCatwalkCollision.Value)
            {
                return;
            }
            if (sceneName != "SampleSceneRelay" && sceneName != "MainMenu")// if loading into a moon
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
            if (sceneName != "SampleSceneRelay" && sceneName != "MainMenu")// if loading into a moon
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
                GameObject cube = GameObject.Find("Environment/HangarShip/Cube");// stray bottom collider which is not in the correct location
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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        [HarmonyPostfix]
        static void OnConnectionClients(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                if (GameObject.Find("Teleporter(Clone)/AnimContainer/PlacementCollider"))
                {
                    doTeleporter = true;
                }
                if (GameObject.Find("InverseTeleporter(Clone)/AnimContainer/PlacementCollider"))
                {
                    doInverse = true;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableClientRpc))]
        [HarmonyPostfix]
        static void TeleporterBuildOnBuy(StartOfRound __instance, int unlockableID)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value && !__instance.IsServer)
            {
                if (unlockableID == 5)
                {
                    doTeleporter = true;
                }
                else if (unlockableID == 19)
                {
                    doInverse = true;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        static void OnUpdate(StartOfRound __instance)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                if (doTeleporter)
                {
                    TeleporterBuildCollision();
                    doTeleporter = false;
                }
                else if (doInverse)
                {
                    TeleporterBuildCollision();
                    doInverse = false;
                }
            }
        }

        static void TeleporterBuildCollision()
        {
            GameObject teleportObj = GameObject.Find("Teleporter(Clone)/AnimContainer/PlacementCollider");
            GameObject inverseTeleportObj = GameObject.Find("InverseTeleporter(Clone)/AnimContainer/PlacementCollider");
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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        [HarmonyPostfix]
        static void TeleporterBuildOnSpawn(StartOfRound __instance, int unlockableIndex)
        {
            if (ScienceBirdTweaks.TinyTeleporterCollision.Value)
            {
                if (unlockableIndex == 5)
                {
                    doTeleporter = true;
                }
                else if (unlockableIndex == 19)
                {
                    doInverse = true;
                }
            }
        }
    }
}
