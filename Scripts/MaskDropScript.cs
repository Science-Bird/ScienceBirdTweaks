using System.Collections;
using System.Collections.Generic;
using ScienceBirdTweaks.Patches;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class MaskDropScript : NetworkBehaviour
    {
        public bool doneRPC = false;
        public static List<MaskInstance> activeMasks = new List<MaskInstance>();

        public void PrepareMaskDropCoroutine(GameObject? prefab, GameObject mask)// prefab passed into coroutine is what mask should be spawned (server only), mask is the object currently worn by enemy
        {
            activeMasks.Add(new MaskInstance(mask, mask.transform.position, mask.transform.rotation));// to handle multiple mask spawns at once, they're pushed into a list
            StartCoroutine(SpawnMaskAfterAnim(prefab, mask));
        }

        [ClientRpc]
        private void SyncMaskValuesClientRpc(int activeMaskIndex, int maskId, int value)// relevant values passed from server to clients
        {
            if (activeMaskIndex >= activeMasks.Count)
            {
                ScienceBirdTweaks.Logger.LogWarning("Failed to find mask on this client! Expect a de-synced item and please report this issue.");
                doneRPC = true;
                return;
            }
            ScienceBirdTweaks.Logger.LogDebug($"Registering mask in clientRPC: {activeMaskIndex}, {maskId}, {value}");
            MaskInstance activeMask = activeMasks[activeMaskIndex];
            activeMask.id = maskId;
            activeMask.value = value;
            activeMask.UpdateTransform();
            activeMask.AddToBufferAndDestroy();// all these values are added into another buffer back in the main patch, using unique networkId to keep track of which mask is which
            activeMasks.RemoveAt(activeMaskIndex);
            MaskDropPatches.patchingMask = true;
            doneRPC = true;
        }
        
        public IEnumerator SpawnMaskAfterAnim(GameObject? prefab, GameObject mask)
        {
            yield return new WaitForSeconds(1.5f);// delay exists to get the final resting position of the mask on the masked enemy (i.e. waiting for it to fall over and complete its death animation)

            MaskInstance activeMask = activeMasks.Find(x => x.wornMask == mask);// find the mask in list matching the one we're currently handling (via checking worn mask object)
            if (activeMask != null)// client rpc has usually destroyed the mask by this point (making it null), so this generally only runs on server
            {
                activeMask.UpdateTransform();
            }
            if (prefab != null && activeMask != null)// server only
            {
                GameObject obj = Object.Instantiate(prefab, activeMask.position, activeMask.rotation, RoundManager.Instance.spawnedScrapContainer);
                obj.GetComponent<NetworkObject>().Spawn();
                GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
                doneRPC = false;
                SyncMaskValuesClientRpc(activeMasks.IndexOf(activeMask), (int)grabbable.GetComponent<NetworkObject>().NetworkObjectId, Mathf.RoundToInt(Random.Range(0.85f,1.15f)*ScienceBirdTweaks.MaskScrapValue.Value));
                while (!doneRPC)
                {
                    yield return null;
                }
                MaskDropPatches.MaskDropSync(obj.GetComponent<HauntedMaskItem>(), (int)grabbable.GetComponent<NetworkObject>().NetworkObjectId, activeMask.position, activeMask.rotation, activeMask.value);// since patched clientrpc won't run on server, the sync function is ran directly here
                grabbable.DiscardItemClientRpc();// artificially induce discard function to get clients to run discard routine
            }
        }
    }



    public class MaskInstance
    {
        public GameObject wornMask;
        public Vector3 position;
        public Quaternion rotation;
        public int id;
        public int value = ScienceBirdTweaks.MaskScrapValue.Value;

        public MaskInstance(GameObject headMask, Vector3 targetPos, Quaternion targetRot, int networkId = -1)
        {
            wornMask = headMask;
            position = targetPos;
            rotation = targetRot;
            id = networkId;
        }

        public void UpdateTransform()// the way this is done means each client will use their own wornMask's position, so even if mask corpse positions are de-synced, the masks will still appear in the correct spots for each client
        {
            if (wornMask != null)
            {
                position = wornMask.transform.position;
                rotation = wornMask.transform.rotation;
            }
        }
        public void AddToBufferAndDestroy()
        {
            ScienceBirdTweaks.Logger.LogDebug("Adding mask to buffer!");
            if (id != -1)
            {
                MaskDropPatches.maskBuffer.Add(id, (position, rotation, value));
                if (wornMask != null)
                {
                    if (wornMask.name == "HeadOni")
                    {
                        Object.Destroy(wornMask);
                    }
                    else
                    {
                        Object.Destroy(wornMask.transform.parent.gameObject);
                    }
                }
            }
        }

    }
}
