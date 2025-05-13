using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class ShipFloodlightInteractionHandler : NetworkBehaviour
    {
        public static ShipFloodlightController controller;
        public ButtonPanelController panelController;
        private bool sendingRPC = false;

        public void Start()// this class has been largely replaced by ButtonPanelController, but still serves some niche RPC calls since FloodlightController is a monobehaviour
        {
            if (ScienceBirdTweaks.FloodlightRotation.Value)
            {
                controller = FindObjectOfType<ShipFloodlightController>();
            }
        }

        [ClientRpc]
        public void LandingSyncClientRpc(bool rotate)
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ShipFloodlightController>();
            }
            if (controller != null)
            {
                StartCoroutine(LandingSyncWait(rotate));
            }
        }

        public IEnumerator LandingSyncWait(bool rotate)
        {
            if (rotate)
            {
                yield return new WaitForSeconds(2f);
            }
            controller._canRotate = rotate;
            controller.RPCsent = false;
        }

        public void ToggleSpinning()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ShipFloodlightController>();
            }
            if (controller != null)
            {
                if (controller._isRotating)
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