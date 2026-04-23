using System.Collections;
using UnityEngine;
using ScienceBirdTweaks.Patches;

namespace ScienceBirdTweaks.Scripts
{
    public class ScanHighlight : MonoBehaviour
    {
        public Transform parentTransform;
        public Renderer[] renderers;
        public Renderer[] extraRenderers;
        public bool blue;
        public bool full = false;

        private void Start()
        {
            if (full)
            {
                StartCoroutine(FadeIn());
            }
        }

        public void LateUpdate()
        {
            transform.position = parentTransform.position;
            transform.rotation = parentTransform.rotation;
        }

        public IEnumerator FadeIn()
        {
            Material[] highlightMatsGreen = [ScanHighlightPatches.greenHologramMat1, ScanHighlightPatches.greenHologramMat2, ScanHighlightPatches.greenHologramMat3, ScanHighlightPatches.greenHologramMat4, ScanHighlightPatches.greenHologramMat];
            Material[] highlightMatsBlue = [ScanHighlightPatches.blueHologramMat1, ScanHighlightPatches.blueHologramMat2, ScanHighlightPatches.blueHologramMat3, ScanHighlightPatches.blueHologramMat4, ScanHighlightPatches.blueHologramMat];
            for (int k = 0; k < 5; k++)
            {
                yield return new WaitForSeconds(0.05f);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j].gameObject.layer == 22) { continue; }
                    Material[] materials = new Material[renderers[j].materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = blue ? highlightMatsBlue[k] : highlightMatsGreen[k];
                    }
                    renderers[j].materials = materials;
                }
                for (int j = 0; j < extraRenderers.Length; j++)
                {
                    if (extraRenderers[j].gameObject.layer == 22) { continue; }
                    Material[] materials = new Material[extraRenderers[j].materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = blue ? highlightMatsBlue[k] : highlightMatsGreen[k];
                    }
                    extraRenderers[j].materials = materials;
                }
            }
        }
    }
}
