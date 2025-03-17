using Unity.Netcode;
using SelfSortingStorage.Cupboard;
using static SelfSortingStorage.Cupboard.SmartMemory;
using System.Collections.Generic;

namespace ScienceBirdTweaks
{
    public class SSSDataRequest : NetworkBehaviour
    {
        public Dictionary<string, int> storedDict = new Dictionary<string, int>();

        [ServerRpc(RequireOwnership = false)]
        public void CollectDataServerRpc()
        {
            SmartCupboard cupboard = UnityEngine.Object.FindObjectOfType<SmartCupboard>();
            if (Instance != null && cupboard != null)
            {
                storedDict = new Dictionary<string, int>();
                ResetDictClientRpc();
                List<List<Data>> itemList = Instance.ItemList;
                foreach (List<Data> rowList in itemList)
                {
                    foreach (Data item in rowList)
                    {
                        string[] splitName = item.Id.Split("/");
                        if (splitName.Length > 1)
                        {
                            if (storedDict.TryAdd(splitName[1].ToLower(), item.Quantity))
                            {
                                ScienceBirdTweaks.Logger.LogDebug($"SERVER: Added {item.Quantity} {splitName[1].ToLower()} to dictionary!");
                                SendDataClientRpc(splitName[1].ToLower(), item.Quantity);
                            }
                        }
                    }
                }
            }
        }

        [ClientRpc]
        public void SendDataClientRpc(string name, int count)
        {
            if (!base.IsServer)
            {
                if (storedDict.TryAdd(name, count))
                {
                    ScienceBirdTweaks.Logger.LogDebug($"CLIENT: Added {count} {name} to dictionary!");
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogError($"Client failure to sync dictionary!");
                }
            }
        }

        [ClientRpc]
        public void ResetDictClientRpc()
        {
            if (!base.IsServer)
            {
                storedDict = new Dictionary<string, int>();
            }
        }
    }
}
