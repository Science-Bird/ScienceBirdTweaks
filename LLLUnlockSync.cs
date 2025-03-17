using Unity.Netcode;
using LethalLevelLoader;

namespace ScienceBirdTweaks
{
    public class LLLUnlockSync : NetworkBehaviour
    {
        public void CheckUnlocks()
        {
            foreach (ExtendedLevel level in PatchedContent.ExtendedLevels)
            {
                CheckUnlocksClientRpc(level.UniqueIdentificationName, level.IsRouteHidden, level.IsRouteLocked);
            }
        }

        [ClientRpc]
        public void CheckUnlocksClientRpc(string uniqueName, bool hidden, bool locked)
        {
            if (base.IsServer) { return; }
            ExtendedLevel target = PatchedContent.ExtendedLevels.Find(x => x.UniqueIdentificationName == uniqueName);
            if (target != null && (target.IsRouteHidden != hidden || target.IsRouteLocked != locked))
            {
                ScienceBirdTweaks.Logger.LogInfo($"Client mismatch with host extended level {uniqueName} detected! Fixing...");
                target.IsRouteHidden = hidden;
                target.IsRouteLocked = locked;
            }
            if (target == null)
            {
                ScienceBirdTweaks.Logger.LogWarning($"Couldn't find moon {uniqueName}!");
            }
        }
    }
}
