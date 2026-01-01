using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ScienceBirdTweaks.Patches;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class NullItemScript : NetworkBehaviour
    {
        public static bool doFix = false;
        public Dictionary<GrabbableObject, Vector3> activeObjects = new Dictionary<GrabbableObject, Vector3>();

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
        public void SpawnReplacementObjectClientRpc(NetworkObjectReference netObjectRef, int scrapValue, Vector3 position, Vector3 fallPosition, Vector3 floorPosition, bool usedUp, Quaternion rotation, bool inShip, bool inElev, bool inFactory, bool nullParent = false)
        {
            if (!base.IsServer)
            {
                StartCoroutine(WaitForSpawn(netObjectRef, scrapValue, position, fallPosition, floorPosition, usedUp, rotation, inShip, inElev, inFactory, nullParent));
            }
        }

        private IEnumerator WaitForSpawn(NetworkObjectReference netObjectRef, int scrapValue, Vector3 position, Vector3 fallPosition, Vector3 floorPosition, bool usedUp, Quaternion rotation, bool inShip, bool inElev, bool inFactory, bool nullParent)
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
            if (nullParent)
            {
                component.gameObject.transform.SetParent(null, false);
            }
            else if (component.gameObject.transform.parent == null)
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
            component.isInShipRoom = inShip;
            component.isInElevator = inElev;
            component.isInFactory = inFactory;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReplacementObjectServerRpc(NetworkObjectReference netObjectRef)
        {
            if (netObjectRef.TryGet(out NetworkObject netObject))
            {
                GrabbableObject grabbable = netObject.GetComponent<GrabbableObject>();
                string name = Regex.Replace(grabbable.gameObject.name, "\\(Clone\\)$", "");
                Item[] replacementItems = Resources.FindObjectsOfTypeAll<Item>().Where(x => x.spawnPrefab != null && x.spawnPrefab.name == name && x.spawnPrefab.GetComponent<NetworkObject>().PrefabIdHash != 0).ToArray();
                if (replacementItems != null && replacementItems.Length > 0)
                {
                    Item properties = replacementItems.First();
                    GameObject newObject = UnityEngine.Object.Instantiate(properties.spawnPrefab, grabbable.transform.position, Quaternion.identity);
                    GrabbableObject component = newObject.GetComponent<GrabbableObject>();
                    component.itemUsedUp = grabbable.itemUsedUp;
                    int value = -1;
                    if (component.itemProperties.isScrap)
                    {
                        value = grabbable.scrapValue;
                        component.SetScrapValue(value);
                    }
                    component.isInShipRoom = grabbable.isInShipRoom;
                    component.isInElevator = grabbable.isInElevator;
                    component.isInFactory = grabbable.isInFactory;
                    component.gameObject.GetComponent<NetworkObject>().Spawn();
                    component.gameObject.transform.SetParent(null, true);

                    component.transform.position += Vector3.up * 0.5f;

                    SetItemValsClientRpc(component.gameObject.GetComponent<NetworkObject>(), component.scrapValue, component.itemUsedUp, component.isInShipRoom, component.isInElevator, component.isInFactory);
                    grabbable.NetworkObject.Despawn();
                    NullItemPatches.triggered = false;
                }
            }
        }

        [ClientRpc]
        public void SetItemValsClientRpc(NetworkObjectReference netObjectRef, int scrapValue, bool usedUp, bool inShip, bool inElev, bool inFactory)
        {
            if (netObjectRef.TryGet(out NetworkObject netObject))
            {
                GrabbableObject grabbable = netObject.GetComponent<GrabbableObject>();
                grabbable.SetScrapValue(scrapValue);
                grabbable.itemUsedUp = usedUp;
                grabbable.isInShipRoom = inShip;
                grabbable.isInElevator = inElev;
                grabbable.isInFactory = inFactory;
            }
        }

        //[ClientRpc]
        //public void DestroyGrabbableClientRpc(GrabbableObject grabbable)
        //{
        //    int slot = -1;
        //    if ((grabbable.isHeld || grabbable.isPocketed) && grabbable.playerHeldBy != null)
        //    {
        //        slot = System.Array.IndexOf(grabbable.playerHeldBy.ItemSlots, grabbable);
        //        if (slot >= 0)
        //        {
        //            grabbable.playerHeldBy.DestroyItemInSlot(slot);
        //        }
        //    }
        //    else
        //    {
        //        Object.Destroy(grabbable.gameObject);
        //    }
        //}

    }
}
