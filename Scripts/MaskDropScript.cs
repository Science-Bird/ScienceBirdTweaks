using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using ScienceBirdTweaks.Patches;
using Steamworks.ServerList;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ScienceBirdTweaks.Scripts
{
    public class MaskDropScript : NetworkBehaviour
    {
        public bool doneRPC = false;
        public static List<MaskInstance> activeMasks = new List<MaskInstance>();

        public void PrepareMaskDropCoroutine(GameObject? prefab, GameObject mask)
        {
            ScienceBirdTweaks.Logger.LogDebug($"Running coroutine...");
            activeMasks.Add(new MaskInstance(mask, mask.transform.position, mask.transform.rotation));
            StartCoroutine(SpawnMaskAfterAnim(prefab, mask));
        }

        [ClientRpc]
        private void SyncMaskValuesClientRpc(int activeMaskIndex, int maskId, int value)
        {
            ScienceBirdTweaks.Logger.LogDebug($"Adding mask to buffer! {activeMaskIndex}");
            MaskInstance activeMask = activeMasks[activeMaskIndex];
            activeMask.id = maskId;
            activeMask.value = value;
            activeMask.UpdateTransform();
            activeMask.AddToBufferAndDestroy();
            activeMasks.RemoveAt(activeMaskIndex);
            MaskDropPatches.patchingMask = true;
            doneRPC = true;
        }
        
        public IEnumerator SpawnMaskAfterAnim(GameObject? prefab, GameObject mask)
        {
            ScienceBirdTweaks.Logger.LogDebug($"Entering coroutine!");
            yield return new WaitForSeconds(1.5f);
            MaskInstance activeMask = activeMasks.Find(x => x.wornMask == mask);
            if (activeMask != null)
            {
                activeMask.UpdateTransform();
                ScienceBirdTweaks.Logger.LogInfo($"Found active mask! {activeMask.position}, {activeMask.rotation.eulerAngles}");
            }
            if (prefab != null && activeMask != null)
            {
                ScienceBirdTweaks.Logger.LogInfo($"Starting after delay!");
                GameObject obj = Object.Instantiate(prefab, activeMask.position, activeMask.rotation, RoundManager.Instance.spawnedScrapContainer);
                obj.GetComponent<NetworkObject>().Spawn();
                GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
                SyncMaskValuesClientRpc(activeMasks.IndexOf(activeMask), (int)grabbable.GetComponent<NetworkObject>().NetworkObjectId, Mathf.RoundToInt(Random.Range(0.85f,1.15f)*ScienceBirdTweaks.MaskScrapValue.Value));
                while (!doneRPC)
                {
                    yield return null;
                }
                ScienceBirdTweaks.Logger.LogDebug("Calling discard RPC!");
                MaskDropPatches.MaskDropSync(obj.GetComponent<HauntedMaskItem>(), (int)grabbable.GetComponent<NetworkObject>().NetworkObjectId, activeMask.position, activeMask.rotation, activeMask.value);
                grabbable.DiscardItemClientRpc();
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

        public void UpdateTransform()
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
                    Object.Destroy(wornMask.transform.parent.gameObject);
                }
            }
        }

    }
}
