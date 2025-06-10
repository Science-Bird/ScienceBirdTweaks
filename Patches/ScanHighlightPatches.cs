using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ScanHighlightPatches
    {
        public static List<GrabbableObject> scannedObjects = new List<GrabbableObject>();
        public static Dictionary<GrabbableObject, GameObject> highlightDict = new Dictionary<GrabbableObject, GameObject>();
        public static Material greenHologramMat;
        public static Material blueHologramMat;
        private static readonly HashSet<System.Type> keepTypes = new HashSet<System.Type> { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer)};
        private static readonly HashSet<System.Type> disableTypes = new HashSet<System.Type> { typeof(AudioSource), typeof(Light), typeof(HDAdditionalLightData), typeof(SkinnedMeshRenderer), typeof(Animator)};
        private static readonly Vector3 scaleFactorUp = new Vector3(1.02f, 1.02f, 1.02f);
        private static readonly Vector3 scaleFactorDown = new Vector3(0.960784f, 0.960784f, 0.960784f);

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        [HarmonyPostfix]
        static void OnGrabItem(GrabbableObject __instance)
        {
            if (ScienceBirdTweaks.ScanHighlights.Value && highlightDict.TryGetValue(__instance, out GameObject value))
            {
                Object.Destroy(value);
                highlightDict.Remove(__instance);
            }
        }

        public static void MaterialSetup(HUDManager HUD, int index)
        {
            if (!ScienceBirdTweaks.ScanHighlights.Value) { return; }

            switch (index)
            {
                case 0:
                    Texture2D hologramTex = (Texture2D)ScienceBirdTweaks.TweaksAssets.LoadAsset("HologramTex");
                    greenHologramMat = new Material(HUD.hologramMaterial);
                    greenHologramMat.SetVector("_MainColor", new Vector4(3f, 30f, 3f, 0f));
                    greenHologramMat.SetVector("_FresnelColor", new Vector4(0.1f, 0.1f, 0.1f, 0.1f));
                    LocalKeyword disableSSR = new LocalKeyword(greenHologramMat.shader, "_DISABLE_SSR_TRANSPARENT");
                    if (disableSSR != null)
                    {
                        greenHologramMat.SetLocalKeyword(disableSSR, false);
                    }
                    greenHologramMat.SetFloat("_ScrollSpeed", 0.04f);
                    greenHologramMat.SetTexture("_HologramScanlines", hologramTex);
                    break;
                case 1:
                    Texture2D hologramTexBlue = (Texture2D)ScienceBirdTweaks.TweaksAssets.LoadAsset("HologramTexBlue");
                    blueHologramMat = new Material(HUD.hologramMaterial);
                    blueHologramMat.SetVector("_MainColor", new Vector4(3f, 3f, 30f, 0f));
                    blueHologramMat.SetVector("_FresnelColor", new Vector4(0.1f, 0.1f, 0.1f, 0.1f));
                    LocalKeyword disableSSRBlue = new LocalKeyword(blueHologramMat.shader, "_DISABLE_SSR_TRANSPARENT");
                    if (disableSSRBlue != null)
                    {
                        blueHologramMat.SetLocalKeyword(disableSSRBlue, false);
                    }
                    blueHologramMat.SetFloat("_ScrollSpeed", 0.04f);
                    blueHologramMat.SetTexture("_HologramScanlines", hologramTexBlue);
                    break;
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateScanNodes))]
        [HarmonyPostfix]
        static void OnScanUpdate(HUDManager __instance)
        {
            if (!ScienceBirdTweaks.ScanHighlights.Value) { return; }

            if (greenHologramMat == null)
            {
                MaterialSetup(__instance, 0);
            }
            if (blueHologramMat == null)
            {
                MaterialSetup(__instance, 1);
            }

            List<GrabbableObject> newScannedObjects = new List<GrabbableObject>();
            for (int i = 0; i < __instance.scanElements.Length; i++)
            {
                if (__instance.scanNodes.Count > 0 && __instance.scanNodes.TryGetValue(__instance.scanElements[i], out var value) && value != null)
                {
                    GrabbableObject grabbable = value.GetComponentInParent<GrabbableObject>();
                    if (grabbable != null)
                    {
                        if ((bool)grabbable.GetComponentInChildren<SkinnedMeshRenderer>())
                        {
                            continue;
                        }
                        if (!scannedObjects.Contains(grabbable))
                        {
                            //ScienceBirdTweaks.Logger.LogDebug($"New scanned object! {grabbable.name}");
                            bool blue = false;
                            if (grabbable.itemProperties != null && !grabbable.itemProperties.isScrap && grabbable.itemProperties.itemId != 14 && grabbable.itemProperties.itemId != 16)
                            {
                                blue = true;
                            }
                            GameObject meshHighlight = DuplicateRenderersWithMaterial(grabbable.gameObject, blue);
                            highlightDict.Add(grabbable, meshHighlight);
                        }
                        newScannedObjects.Add(grabbable);
                    }
                }
            }
            List<GrabbableObject> remainingObjects = scannedObjects.Except(newScannedObjects).ToList();
            if (remainingObjects.Count > 0)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"Objects to remove: {remainingObjects.Count}");
                for (int i = 0; i < remainingObjects.Count; i++)
                {
                    if (highlightDict.TryGetValue(remainingObjects[i], out GameObject value))
                    {
                        Object.Destroy(value);
                        highlightDict.Remove(remainingObjects[i]);
                    }
                }
            }
            scannedObjects = new List<GrabbableObject>(newScannedObjects);
            newScannedObjects.Clear();
        }

        public static GameObject DuplicateRenderersWithMaterial(GameObject sourceObject, bool blue)
        {
            GameObject duplicate = Object.Instantiate(sourceObject, sourceObject.transform.position, sourceObject.transform.rotation, sourceObject.transform.parent);
            duplicate.name = sourceObject.name + "_ScanMesh";

            Component[] allComponents = duplicate.GetComponentsInChildren<Component>(true);
            foreach (Component component in allComponents)
            {
                if (component != null && !keepTypes.Contains(component.GetType()))
                {
                    if (component is GrabbableObject grabbable)
                    {
                        grabbable.radarIcon = null;
                    }
                    if (disableTypes.Contains(component.GetType()) && component is Behaviour componentBehaviour)
                    {
                        componentBehaviour.enabled = false;
                    }
                    else
                    {
                        Object.DestroyImmediate(component);
                    }
                }
            }

            Renderer[] renderers = duplicate.GetComponentsInChildren<Renderer>();
            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j].gameObject.layer == 22) { continue; }
                Material[] materials = new Material[renderers[j].materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = blue ? blueHologramMat : greenHologramMat;
                }
                renderers[j].materials = materials;

                //Vector3 boundsNormalized = Vector3.Normalize(renderers[j].bounds.size);
                //float min = 100f;
                //for (int i = 0; i < 3; i++)
                //{
                //    if (Mathf.Abs(boundsNormalized[i]) < min)
                //    {
                //        min = Mathf.Abs(boundsNormalized[i]);
                //    }
                //}
                //float scaleX = renderers[j].gameObject.transform.localScale.x * (1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.x) / min)));
                //float scaleY = renderers[j].gameObject.transform.localScale.y * (1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.y) / min)));
                //float scaleZ = renderers[j].gameObject.transform.localScale.z * (1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.z) / min)));
                //ScienceBirdTweaks.Logger.LogDebug($"Calculated scale ({sourceObject.name}): {renderers[j].gameObject.transform.localScale} > ({1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.x) / min))}, {1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.y) / min))}, {1f + 0.03f * (1 / (Mathf.Abs(boundsNormalized.z) / min))}) > ({scaleX}, {scaleY}, {scaleZ})");
                //renderers[j].gameObject.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }

            duplicate.transform.localScale = Vector3.Scale(duplicate.transform.localScale, scaleFactorUp);

            GameObject extraLayer = Object.Instantiate(duplicate, duplicate.transform.position, duplicate.transform.rotation, duplicate.transform);
            extraLayer.transform.localScale = scaleFactorDown;

            Renderer[] extraRenderers = extraLayer.GetComponentsInChildren<Renderer>();
            for (int j = 0; j < extraRenderers.Length; j++)
            {
                if (extraRenderers[j].gameObject.layer == 22) { continue; }
                Material[] materials = new Material[extraRenderers[j].materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = blue ? blueHologramMat : greenHologramMat;
                }
                extraRenderers[j].materials = materials;
            }

            return duplicate;
        }
    }
}
