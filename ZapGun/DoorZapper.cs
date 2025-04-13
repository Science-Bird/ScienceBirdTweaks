using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace ScienceBirdTweaks.ZapGun
{
    public class DoorZapper : NetworkBehaviour, IShockableWithGun
    {
        TerminalAccessibleObject terminalObj;
        public bool stunned = false;
        public bool masterZappable = true;

        private void Start()
        {
            gameObject.layer = 21;
            terminalObj = GetComponent<TerminalAccessibleObject>();
            masterZappable = ScienceBirdTweaks.ZappableBigDoors.Value && ScienceBirdTweaks.ZapGunRework.Value;
        }

        bool IShockableWithGun.CanBeShocked()
        {
            return !terminalObj.isDoorOpen && !stunned && masterZappable;
        }

        float IShockableWithGun.GetDifficultyMultiplier()
        {
            return 0.8f;
        }

        NetworkObject IShockableWithGun.GetNetworkObject()
        {
            return NetworkObject;
        }

        Vector3 IShockableWithGun.GetShockablePosition()
        {
            return gameObject.transform.position + new Vector3(0, 2.5f, 0);
        }

        Transform IShockableWithGun.GetShockableTransform()
        {
            return gameObject.transform;
        }

        void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
        {
            terminalObj.SetDoorOpen(true);
            stunned = true;
        }

        void IShockableWithGun.StopShockingWithGun()
        {
            stunned = false;
            terminalObj.SetDoorOpen(false);
        }
    }
}
