using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace ScienceBirdTweaks.Scripts
{
    public class ShipFloodlightController : MonoBehaviour
    {
        public float rotationSpeed = 45.0f;

        private const string ParentName = "ShipLightsPost";
        private const string PivotChildName = "Cube.006";
        private readonly List<string> SiblingToRotate = new List<string> { "Floodlight1", "Floodlight2" };

        private Transform _parentTransform;
        private Transform _pivotTransform;
        private List<Transform> _rotationList = new List<Transform>();
        List<Light> _shipFloodlightLights = new List<Light>();
        private StartOfRound _startOfRoundInstance;
        private bool _isRotating = false;
        private bool _initialized = false;
        private bool _initialStateSet = false;
        private bool _canRotate = false;

        private Dictionary<Transform, TransformState> _originalStates = new Dictionary<Transform, TransformState>();

        private struct TransformState
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        void Start()
        {
            _startOfRoundInstance = GetComponent<StartOfRound>();

            if (_startOfRoundInstance == null)
                _startOfRoundInstance = StartOfRound.Instance;

            if (_startOfRoundInstance == null)
                ScienceBirdTweaks.Logger.LogError("Failed to get StartOfRound instance! Spinner cannot check landing status.");
            else
                ScienceBirdTweaks.Logger.LogInfo("ShipFloodlightController Start: Got StartOfRound reference. Waiting for initialization and landing.");

            StartSpinning();
        }

        void Update()
        {
            if (!_initialized)
            {
                if (_parentTransform == null)
                {
                    GameObject parentObject = GameObject.Find(ParentName);
                    if (parentObject != null)
                    {
                        _parentTransform = parentObject.transform;
                        ScienceBirdTweaks.Logger.LogDebug($"Found parent container: '{_parentTransform.name}'.");
                    }
                    else return;
                }

                if (_pivotTransform == null)
                {
                    _pivotTransform = _parentTransform.Find(PivotChildName);
                    if (_pivotTransform != null)
                    {
                        ScienceBirdTweaks.Logger.LogDebug($"Found pivot child: '{_pivotTransform.name}'.");
                        if (!_rotationList.Contains(_pivotTransform))
                            _rotationList.Add(_pivotTransform);
                    }
                    else return;
                }

                if (_rotationList.Count < 3)
                {
                    foreach (string sibling in SiblingToRotate)
                    {
                        Transform siblingTransform = _parentTransform.Find(sibling);
                        if (siblingTransform != null)
                        {
                            _shipFloodlightLights.Add(siblingTransform.GetComponentInChildren<Light>(true));
                            _rotationList.Add(siblingTransform);
                            ScienceBirdTweaks.Logger.LogDebug($"Found sibling: '{siblingTransform.name}'.");
                        }
                        else return;
                    }
                }
                else if (_rotationList.Count == 3) // cube.006, Floodlight1, Floodlight2
                {
                    ScienceBirdTweaks.Logger.LogDebug($"All objects found: {_rotationList.Count} objects.");

                    _originalStates.Clear();

                    foreach (Transform transformObject in _rotationList)
                    {
                        if (transformObject != null)
                        {
                            _originalStates[transformObject] = new TransformState
                            {
                                localPosition = transformObject.localPosition,
                                localRotation = transformObject.localRotation
                            };

                            ScienceBirdTweaks.Logger.LogDebug($"Stored initial state for {transformObject.name}: Pos={transformObject.localPosition}, Rot={transformObject.localRotation.eulerAngles}");
                        }
                        else
                            ScienceBirdTweaks.Logger.LogWarning("Attempted to store initial rotation for a null transform in _rotationList.");
                    }

                    _initialized = true;
                }
                else return;
            }


            if (_initialized && ScienceBirdTweaks.FloodlightRotation.Value)
            {
                bool isLanded = _startOfRoundInstance != null && _startOfRoundInstance.shipHasLanded;
                _canRotate = isLanded;

                if (_pivotTransform == null)
                    return;

                Vector3 pivotPoint = _pivotTransform.position;

                if (_canRotate)
                {
                    if (_isRotating)
                    {
                        foreach (Transform rotatingT in _rotationList)
                        {
                            if (rotatingT != null)
                                rotatingT.RotateAround(pivotPoint, Vector3.up.normalized, rotationSpeed * Time.deltaTime);
                            else
                                ScienceBirdTweaks.Logger.LogWarning($"Rotating sibling is null! Skipping rotation.");
                        }
                    }
                }
            }
        }

        public void StartSpinning()
        {
            _isRotating = true;
            if (!this.enabled)
                this.enabled = true;

            if (_initialStateSet && _canRotate)
                ScienceBirdTweaks.Logger.LogInfo($"Rotation enabled.");
            else if
                (_initialStateSet) ScienceBirdTweaks.Logger.LogDebug($"Rotation enabled, waiting for ship to land.");
            else
                ScienceBirdTweaks.Logger.LogDebug($"Rotation enabled, waiting for object initialization, initial state setup, and ship landing.");
        }

        public void StopSpinning()
        {
            _isRotating = false;

            if
                (_parentTransform != null) ScienceBirdTweaks.Logger.LogInfo($"Rotation disabled.");
            else
                ScienceBirdTweaks.Logger.LogInfo($"Rotation explicitly disabled (target may not have been found).");
        }

        public void SetRotationSpeed(float newSpeed) {
            rotationSpeed = newSpeed;
        }

        public void SetFloodlightData(float intensity, float angle, float range)
        {
            float dimmingFactor = intensity / 2275.72f;
            float materialIntensity = dimmingFactor * 12.05f;

            if (_shipFloodlightLights.Count > 0)
            {
                foreach (Light light in _shipFloodlightLights)
                {
                    if (light == null) continue;

                    GameObject lightGameObject = light.gameObject;

                    lightGameObject.TryGetComponent<HDAdditionalLightData>(out var FloodlightData);

                    if (FloodlightData != null)
                    {
                        FloodlightData.SetIntensity(intensity);
                        FloodlightData.SetSpotAngle(angle, 78.3f); // 2nd value is % of inner angle
                        FloodlightData.SetRange(range);
                    }

                    Transform parentTransform = light.transform.parent;

                    Renderer parentRenderer = parentTransform.GetComponent<Renderer>();

                    if (parentRenderer == null) continue;

                    ScienceBirdTweaks.Logger.LogDebug($"Found parent renderer '{parentRenderer.name}'. Processing its materials...");


                    Material[] materials = parentRenderer.materials;

                    foreach (Material mat in materials)
                    {
                        if (mat == null) continue;

                        if (!mat.IsKeywordEnabled("_EMISSIVE_COLOR_MAP")) continue;

                        ScienceBirdTweaks.Logger.LogDebug($"---> Found mat on renderer '{parentRenderer.name}'...");

                        Color originalEmissiveColor = mat.GetColor(Shader.PropertyToID("_EmissiveColor"));

                        ScienceBirdTweaks.Logger.LogDebug($"---> Initial _EmissiveColor : {originalEmissiveColor}");

                        Color dimmedEmissiveColor = new Color(
                            9.026f * dimmingFactor,
                            8.695f * dimmingFactor,
                            7.750f * dimmingFactor,
                            12.05f * dimmingFactor);

                        ScienceBirdTweaks.Logger.LogDebug($"---> Dimmed _EmissiveColor : {dimmedEmissiveColor}");

                        mat.SetFloat(Shader.PropertyToID("_EmissiveIntensity"), dimmedEmissiveColor.a);
                        mat.SetColor(Shader.PropertyToID("_EmissiveColor"), dimmedEmissiveColor);
                    }

                    parentRenderer.materials = materials;
                }
            }
            else
            {
                ScienceBirdTweaks.Logger.LogWarning("No floodlight lights found to set data.");
            }
        }

        public void ResetFloodlightLights()
        {
            SetFloodlightData((float)ScienceBirdTweaks.FloodLightIntensity.Value, (float)ScienceBirdTweaks.FloodLightAngle.Value, (float)ScienceBirdTweaks.FloodLightRange.Value);

            if (ScienceBirdTweaks.FloodlightRotation.Value) // Reset rotations & positions once in orbit to prevent drift
            {
                ScienceBirdTweaks.Logger.LogInfo("Resetting floodlight position / rotation.");

                if (ScienceBirdTweaks.FloodlightRotation.Value)
                {
                    ScienceBirdTweaks.Logger.LogInfo("Resetting floodlight rotation.");

                    if (_originalStates.Count == 0)
                    {
                        ScienceBirdTweaks.Logger.LogWarning("Original rotations not captured or dictionary empty. Cannot reset rotation state.");
                        return;
                    }

                    //if (!_initialStateSet || _originalRotations.Count == 0)
                    //{
                    //    ScienceBirdTweaks.Logger.LogWarning("Original rotations not captured or dictionary empty. Cannot reset rotation state.");
                    //    return;
                    //}

                    ScienceBirdTweaks.Logger.LogDebug($"Resetting floodlight rotation for : {_originalStates}");

                    foreach (KeyValuePair<Transform, TransformState> entry in _originalStates)
                    {
                        Transform t = entry.Key;
                        TransformState originalState = entry.Value;

                        if (t != null)
                        {
                            t.localPosition = originalState.localPosition;
                            t.localRotation = originalState.localRotation;
                            ScienceBirdTweaks.Logger.LogDebug($"Reset {t.name} to Pos={t.localPosition}, Rot={t.localRotation.eulerAngles}");
                        }
                        else
                        {
                            ScienceBirdTweaks.Logger.LogWarning($"Attempted to reset rotation on a null transform (originally stored).");
                        }
                    }
                    ScienceBirdTweaks.Logger.LogInfo("Floodlight rotation reset complete.");
                }
                else
                {
                    ScienceBirdTweaks.Logger.LogInfo("Floodlight rotation disabled in config, skipping rotation reset.");
                }
            }
        }
    }
}