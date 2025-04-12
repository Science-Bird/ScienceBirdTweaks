using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class ShipLightsSpinner : MonoBehaviour
    {
        public float rotationSpeed = 45.0f;

        private const string ParentName = "ShipLightsPost";
        private const string PivotChildName = "Cube.006";
        private readonly List<string> SiblingNamesToRotate = new List<string> { "Floodlight1", "Floodlight2" };

        private Transform _parentTransform;
        private Transform _pivotTransform;
        private List<Transform> _rotatingSiblings = new List<Transform>();
        private StartOfRound _startOfRoundInstance;
        private bool _isRotating = false;
        private bool _initialized = false;
        private bool _initialStateSet = false;
        private bool _canRotate = false;


        void Start()
        {
            _startOfRoundInstance = GetComponent<StartOfRound>();

            if (_startOfRoundInstance == null)
                _startOfRoundInstance = StartOfRound.Instance;

            if (_startOfRoundInstance == null)
                ScienceBirdTweaks.Logger.LogError("Failed to get StartOfRound instance! Spinner cannot check landing status.");
            else
                ScienceBirdTweaks.Logger.LogInfo("ShipLightsSpinner Start: Got StartOfRound reference. Waiting for initialization and landing.");

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

                if (_parentTransform != null)
                {
                    bool allFound = true; // can probably kill this, will see later
                    _rotatingSiblings.Clear(); // can probably also kill this for to prevent re-searching :clueless:

                    if (_pivotTransform == null)
                    {
                        _pivotTransform = _parentTransform.Find(PivotChildName);
                        if (_pivotTransform != null)
                        {
                            ScienceBirdTweaks.Logger.LogDebug($"Found pivot child: '{_pivotTransform.name}'.");
                            if (!_rotatingSiblings.Contains(_pivotTransform))
                                _rotatingSiblings.Add(_pivotTransform);
                        }
                        else
                            allFound = false;
                    }

                    // This is cursed and I dislike it. Will probably hardcode for just the 2 floodlights using .Find() path
                    int expectedOtherSiblings = SiblingNamesToRotate.Count;
                    int foundOtherSiblingsCount = _rotatingSiblings.Count(t => t != null && t != _pivotTransform);

                    if (foundOtherSiblingsCount < expectedOtherSiblings)
                    {
                        foreach (string siblingName in SiblingNamesToRotate)
                        {
                            bool alreadyFound = _rotatingSiblings.Any(t => t != null && t.name == siblingName);
                            if (!alreadyFound)
                            {
                                Transform sibling = _parentTransform.Find(siblingName);
                                if (sibling != null)
                                {
                                    _rotatingSiblings.Add(sibling);
                                    foundOtherSiblingsCount = _rotatingSiblings.Count(t => t != null && t != _pivotTransform);
                                }
                                else
                                    allFound = false;
                            }
                        }
                    }

                    if (_pivotTransform != null && _rotatingSiblings.Count >= (expectedOtherSiblings + 1) && allFound)
                    {
                        _initialized = true;
                        ScienceBirdTweaks.Logger.LogInfo($"Initialization complete. Found Parent, Pivot '{PivotChildName}', and {expectedOtherSiblings} other rotating siblings.");
                    }
                    else
                    {
                        if (_parentTransform != null && Time.frameCount % 60 == 0)
                            ScienceBirdTweaks.Logger.LogWarning($"Still waiting for all siblings to be found under '{_parentTransform.name}'. Found {_rotatingSiblings.Count} / {expectedOtherSiblings + 1} required transforms.");

                        return;
                    }
                }
                else { return; }
            }


            if (_initialized)
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
                        foreach (Transform rotatingT in _rotatingSiblings)
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
    }
}