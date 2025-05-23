using WesleyMoonScripts.Components;
using Unity.Netcode;
using System.Reflection;
using ScienceBirdTweaks.ModPatches;

namespace ScienceBirdTweaks.Scripts
{
    public class WesleyTapeSkip : NetworkBehaviour
    {
        public void StopTape()
        {
            ScienceBirdTweaks.Logger.LogDebug("Stop tape called!");
            LevelCassetteLoader loader = TapeSkipPatches.currentLoader;// get cassette loader from the patch class
            if (loader != null)
            {
                StopTapeServerRpc();
            }
            else// fallback logic
            {
                ScienceBirdTweaks.Logger.LogWarning("Loader not obtained from patch, searching manually...");
                loader = UnityEngine.Object.FindObjectOfType<LevelCassetteLoader>();
                if (loader != null)
                {
                    StopTapeServerRpc();
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogError("Couldn't find loader in scene, exiting skip procedure.");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopTapeServerRpc()
        {
            StopTapeClientRpc();
        }

        [ClientRpc]
        public void StopTapeClientRpc()
        {
            ScienceBirdTweaks.Logger.LogDebug("Stopping tape early...");
            LevelCassetteLoader loader = TapeSkipPatches.currentLoader;
            if (loader != null)
            {
                MethodInfo method = typeof(LevelCassetteLoader).GetMethod("TapeEnded", BindingFlags.NonPublic | BindingFlags.Instance);// grabs the "end tape" method and runs it
                method.Invoke(loader, new object[] { });
            }
        }
    }
}
