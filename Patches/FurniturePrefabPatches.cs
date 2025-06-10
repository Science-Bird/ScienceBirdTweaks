using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using JLL.Components;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class FurniturePrefabPatches
    {

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void FurnitureTagPatch(GameNetworkManager __instance)
        {
            if (!ScienceBirdTweaks.ApplianceInteractionFixes.Value) { return; }

            AutoParentToShip[] prefabFurniture = UnityEngine.Resources.FindObjectsOfTypeAll<AutoParentToShip>().Where(x => x.unlockableID == 28 || x.unlockableID == 30).ToArray();
            foreach (AutoParentToShip furniture in prefabFurniture)
            {
                Transform furnitureTransform = furniture.gameObject.transform;
                switch (furniture.unlockableID)
                {
                    case 28:
                        Transform microwaveTransform = furnitureTransform.Find("MicrowaveBody");
                        if (microwaveTransform != null)
                        {
                            microwaveTransform.gameObject.layer = 8;
                        }
                        break;
                    case 30:
                        Transform[] fridgeTransforms = [furnitureTransform.Find("FridgeBody"), furnitureTransform.Find("ObjectPlacements")];
                        if (fridgeTransforms[0] != null)
                        {
                            foreach (Transform transform in fridgeTransforms[0].GetComponentsInChildren<Transform>())
                            {
                                if (transform.gameObject.name != "Cube" && transform.gameObject.name != "FridgeMagnets (1)" && transform.gameObject.layer != 9)
                                {
                                    transform.gameObject.layer = 8;
                                }
                            }
                        }
                        if (fridgeTransforms[1] != null)
                        {
                            foreach (Transform transform in fridgeTransforms[1].GetComponentsInChildren<Transform>())
                            {
                                if (transform.gameObject.name.Contains("Cube") && transform.gameObject.GetComponent<BoxCollider>())
                                {
                                    BoxCollider placementCollider = transform.gameObject.GetComponent<BoxCollider>();
                                    BoxCollider newCollision = transform.gameObject.AddComponent<BoxCollider>();
                                    newCollision.isTrigger = false;
                                    newCollision.center = placementCollider.center;
                                    newCollision.size = placementCollider.size;
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
