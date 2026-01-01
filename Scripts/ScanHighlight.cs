using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class ScanHighlight : MonoBehaviour
    {
        public Transform parentTransform;

        public void LateUpdate()
        {
            transform.position = parentTransform.position;
            transform.rotation = parentTransform.rotation;
        }
    }
}
