using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using ScienceBirdTweaks.ModPatches;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ScanHighlightPatches
    {
        public static List<GrabbableObject> scanned = new List<GrabbableObject>();
        public static Dictionary<GrabbableObject, GameObject> highlights = new Dictionary<GrabbableObject, GameObject>();
        public static Material greenHologramMat;
        public static Material blueHologramMat;
        private static readonly HashSet<System.Type> keepTypes = new HashSet<System.Type> { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer)};
        private static readonly HashSet<System.Type> disableTypes = new HashSet<System.Type> { typeof(AudioSource), typeof(Light), typeof(HDAdditionalLightData), typeof(SkinnedMeshRenderer), typeof(Animator)};
        private static readonly Vector3 scaleFactorUp = new Vector3(1.02f, 1.02f, 1.02f);
        private static readonly Vector3 scaleFactorDown = new Vector3(0.960784f, 0.960784f, 0.960784f);

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        [HarmonyPrefix]
        static void OnGrabItem(GrabbableObject __instance)
        {
            if (!ScienceBirdTweaks.ScanHighlights.Value) { return; }

            if (!ScienceBirdTweaks.test2Present && highlights.TryGetValue(__instance, out GameObject value1))
            {
                Object.Destroy(value1);
                scanned.Remove(__instance);
                highlights.Remove(__instance);
            }
            else if (ScienceBirdTweaks.test2Present && GoodItemScanPatches.highlights.TryGetValue(__instance, out GameObject value2))
            {
                Object.Destroy(value2);
                GoodItemScanPatches.scanned.Remove(__instance);
                GoodItemScanPatches.highlights.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.PutObjectInBagLocalClient))]
        [HarmonyPrefix]
        static void PutItemInBeltBag(BeltBagItem __instance, GrabbableObject gObject)
        {
            if (!ScienceBirdTweaks.ScanHighlights.Value || gObject == null) { return; }

            if (!ScienceBirdTweaks.test2Present && highlights.TryGetValue(gObject, out GameObject value1))
            {
                Object.Destroy(value1);
                scanned.Remove(gObject);
                highlights.Remove(gObject);
            }
            else if (ScienceBirdTweaks.test2Present && GoodItemScanPatches.highlights.TryGetValue(gObject, out GameObject value2))
            {
                Object.Destroy(value2);
                GoodItemScanPatches.scanned.Remove(gObject);
                GoodItemScanPatches.highlights.Remove(gObject);
            }
        }

        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Start))]
        [HarmonyPostfix]
        [HarmonyAfter("ClaySurgeonMod")]
        [HarmonyPriority(-10000)]
        static void MaterialSetupOnStart(QuickMenuManager __instance)
        {
            if (!ScienceBirdTweaks.ScanHighlights.Value) { return; }
            HDRenderPipelineAsset assetHDRP = QualitySettings.renderPipeline as HDRenderPipelineAsset;
            if (assetHDRP != null)
            {
                RenderPipelineSettings settings = assetHDRP.currentPlatformRenderPipelineSettings;
                settings.supportMotionVectors = false;// messes with the shader if enabled by ClaySurgeonOverhaul
                assetHDRP.currentPlatformRenderPipelineSettings = settings;
            }
            if (HUDManager.Instance != null)
            {
                MaterialSetup(HUDManager.Instance.hologramMaterial, 0);
                MaterialSetup(HUDManager.Instance.hologramMaterial, 1);
            }
        }

        public static void MaterialSetup(Material holoMat, int index)
        {
            switch (index)
            {
                // bunch of shader bullshit to make the hologram shader used for "scrap collected" models look decent when used for highlight models
                // I should probably make my own custom shader instead but this works for now

                case 0:// green (scrap)
                    Texture2D hologramTex = (Texture2D)ScienceBirdTweaks.TweaksAssets.LoadAsset("HologramTex");
                    greenHologramMat = new Material(holoMat);
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
                case 1:// blue (equipment)
                    Texture2D hologramTexBlue = (Texture2D)ScienceBirdTweaks.TweaksAssets.LoadAsset("HologramTexBlue");
                    blueHologramMat = new Material(holoMat);
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
            if (!ScienceBirdTweaks.ScanHighlights.Value || ScienceBirdTweaks.test2Present) { return; }

            if (greenHologramMat == null)
            {
                MaterialSetup(__instance.hologramMaterial, 0);
            }
            if (blueHologramMat == null)
            {
                MaterialSetup(__instance.hologramMaterial, 1);
            }

            List<GrabbableObject> newScannedObjects = ComputeNewScannedObjects(highlights, scanned, __instance.scanNodes.Values.ToList());
            scanned = new List<GrabbableObject>(newScannedObjects);// new scanned objects becomes the regular scanned list and then the cycle repeats next scan
        }

        // general method used by both vanilla scan routine and GoodItemScan routine
        public static List<GrabbableObject> ComputeNewScannedObjects(Dictionary<GrabbableObject, GameObject> highlightDict, List<GrabbableObject> scannedObjects, List<ScanNodeProperties> scanNodeList)
        {
            //ScienceBirdTweaks.Logger.LogDebug($"Starting scan of {scanNodeList.Count} objects");
            List<GrabbableObject> newScannedObjects = new List<GrabbableObject>();
            foreach (ScanNodeProperties scanProps in scanNodeList)
            {
                if (scanProps == null) { continue; }
                GameObject targetObj = scanProps.gameObject;
                GrabbableObject grabbable = targetObj.GetComponentInParent<GrabbableObject>();
                while (grabbable == null)// find grabbable object through scan node parents
                {
                    if (targetObj.transform.parent != null)
                    {
                        targetObj = targetObj.transform.parent.gameObject;
                    }
                    else
                    {
                        break;
                    }
                    grabbable = targetObj.GetComponentInParent<GrabbableObject>();
                }
                if (grabbable != null)
                {
                    if ((bool)grabbable.GetComponentInChildren<SkinnedMeshRenderer>() || (grabbable.itemProperties != null && grabbable.itemProperties.itemId == 16))// skinned mesh renderers and radar boosters are fucked
                    {
                        continue;
                    }
                    if (!scannedObjects.Contains(grabbable))// if an object doesnt already have a highlight model, add one
                    {
                        //ScienceBirdTweaks.Logger.LogDebug($"New scanned object! {grabbable.name}");
                        bool blue = false;
                        if (grabbable.itemProperties != null && !grabbable.itemProperties.isScrap && grabbable.itemProperties.itemId != 14)// keys and radar boosters have green scan nodes so they're excluded from the blue equipment thingy
                        {
                            blue = true;
                        }
                        GameObject meshHighlight = ScanHighlightPatches.DuplicateRenderersWithMaterial(grabbable.gameObject, blue);
                        highlightDict.Add(grabbable, meshHighlight);
                    }
                    newScannedObjects.Add(grabbable);
                }
            }
            List<GrabbableObject> remainingObjects = scannedObjects.Except(newScannedObjects).ToList();// all objects which are missing compared to last scan
            if (remainingObjects.Count > 0)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"Objects to remove: {remainingObjects.Count}");
                for (int i = 0; i < remainingObjects.Count; i++)
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Removing: {remainingObjects[i].name}");
                    if (highlightDict.TryGetValue(remainingObjects[i], out GameObject value))
                    {
                        Object.Destroy(value);
                        highlightDict.Remove(remainingObjects[i]);
                    }
                }
            }
            return newScannedObjects;
        }

        public static GameObject DuplicateRenderersWithMaterial(GameObject sourceObject, bool blue)
        {
            GameObject duplicate = Object.Instantiate(sourceObject, sourceObject.transform.position, sourceObject.transform.rotation, sourceObject.transform.parent);
            duplicate.name = sourceObject.name + "_ScanMesh";

            Component[] allComponents = duplicate.GetComponentsInChildren<Component>(true);
            foreach (Component component in allComponents)// clean most of the non-rendering components
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
