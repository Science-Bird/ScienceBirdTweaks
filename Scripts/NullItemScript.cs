using System.Linq;
using HarmonyLib;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ScienceBirdTweaks.Patches;
using System.ComponentModel;

namespace ScienceBirdTweaks.Scripts
{
    public class NullItemScript : NetworkBehaviour
    {
        public static bool doFix = false;
        public Dictionary<GrabbableObject,Vector3> activeObjects = new Dictionary<GrabbableObject,Vector3>();

        public static NullItemScript Instance { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                UnityEngine.Object.Destroy(Instance.gameObject);
            }
        }

        [ClientRpc]
        public void SpawnReplacementObjectClientRpc(NetworkObjectReference netObjectRef, int scrapValue, Vector3 position, Vector3 fallPosition, Vector3 floorPosition, bool usedUp, Quaternion rotation)
        {
            if (!base.IsServer)
            {
                StartCoroutine(WaitForSpawn(netObjectRef, scrapValue, position, fallPosition, floorPosition, usedUp, rotation));
            }
        }

        private IEnumerator WaitForSpawn(NetworkObjectReference netObjectRef, int scrapValue, Vector3 position, Vector3 fallPosition, Vector3 floorPosition, bool usedUp, Quaternion rotation)
        {
            NetworkObject netObject = null;
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < 8f && !netObjectRef.TryGet(out netObject))
            {
                yield return new WaitForSeconds(0.03f);
            }
            if (netObject == null)
            {
                ScienceBirdTweaks.Logger.LogError("Failed to recieve replacement object on client!");
                yield break;
            }
            yield return new WaitForEndOfFrame();
            GrabbableObject component = netObject.GetComponent<GrabbableObject>();
            if (component.gameObject.transform.parent == null)
            {
                component.gameObject.transform.SetParent(StartOfRound.Instance.elevatorTransform, false);
            }
            component.transform.position = position;
            component.transform.rotation = rotation;
            BridgePatches.SetGrabbableFall(component);
            component.itemUsedUp = usedUp;
            if (scrapValue >= 0 && component.itemProperties.isScrap)
            {
                component.SetScrapValue(scrapValue);
            }
            component.isInShipRoom = true;
            component.isInElevator = true;
            component.isInFactory = false;
        }
    }
}
