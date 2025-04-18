using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class ShipFloodlightInteractionHandler : NetworkBehaviour
    {
        public static ShipFloodlightController controller;
        public Animator interactAnimator;
        private bool sendingRPC = false;

        public void Start()
        {
            controller = FindObjectOfType<ShipFloodlightController>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleSpinningServerRpc()
        {
            ToggleSpinningClientRpc(); 
        }


        [ClientRpc]
        public void ToggleSpinningClientRpc()
        {
            if (sendingRPC)
            {
                sendingRPC = false;
            }
            else
            {
                ToggleSpinning();
            }
        }

        public void ToggleSpinningLocal()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ShipFloodlightController>();
            }
            if (controller != null)
            {
                ToggleSpinning();
                sendingRPC = true;
                ToggleSpinningServerRpc();
            }
        }

        public void ToggleSpinning()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ShipFloodlightController>();
            }
            if (controller != null)
            {
                if (interactAnimator.GetBool("on"))
                {
                    controller.StopSpinning();
                }
                else
                {
                    controller.StartSpinning();
                }
            }
        }
    }
}