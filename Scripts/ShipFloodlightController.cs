using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace ScienceBirdTweaks.Scripts
{
    public class ShipFloodlightController : MonoBehaviour
    {
        public float rotationSpeed = ScienceBirdTweaks.FloodLightRotationSpeed.Value;

        private const string ParentName = "ShipLightsPost";
        private const string PivotChildName = "Cube.006";
        private readonly List<string> SiblingToRotate = new List<string> { "Floodlight1", "Floodlight2" };

        private Transform _parentTransform;
        private Transform _pivotTransform;
        private List<Transform> _rotationList = new List<Transform>();
        List<Light> _shipFloodlightLights = new List<Light>();
        private StartOfRound _startOfRoundInstance;
        private TimeOfDay _timeOfDayInstance;
        public bool _isRotating = false;
        private bool _initialized = false;
        private bool _initialStateSet = false;
        private bool _canRotate = false;
        private Animator interactAnimator;

        private Vector3 playerPos = Vector3.zero;
        private Vector3 enemyPos = Vector3.zero;
        private float timeSinceLastCheck = 0f;
        private const float refreshInterval = 1f;
        private bool awaitingSpin = false;
        private bool rotatingLastFrame = false;

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

            if (_timeOfDayInstance == null)
                _timeOfDayInstance = TimeOfDay.Instance;

            if (_startOfRoundInstance == null)
                ScienceBirdTweaks.Logger.LogError("Failed to get StartOfRound instance! Spinner cannot check landing status.");
            else
                ScienceBirdTweaks.Logger.LogDebug("ShipFloodlightController Start: Got StartOfRound reference. Waiting for initialization and landing.");
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
                bool isLanded = _startOfRoundInstance != null && _startOfRoundInstance.shipHasLanded && _timeOfDayInstance != null && (_timeOfDayInstance.currentDayTime / _timeOfDayInstance.totalTime) > 0.13f;// wait until certain time to ensure players synced
                _canRotate = isLanded;

                if (_pivotTransform == null)
                    return;

                Vector3 pivotPoint = _pivotTransform.position;

                if (_canRotate)
                {
                    if (_isRotating)
                    {
                        timeSinceLastCheck += Time.deltaTime;

                        if (ScienceBirdTweaks.FloodlightPlayerFollow.Value && timeSinceLastCheck >= refreshInterval)
                        {
                            playerPos = GetClosestPlayerPosition(_pivotTransform);
                            timeSinceLastCheck = 0f;
                        }

                        //Vector3 enemyPos = GetClosestWhitelistedEnemy(_pivotTransform)?.transform.position ?? Vector3.zero;

                        if (playerPos != Vector3.zero)
                        {
                            Vector3 toPlayer = playerPos - _pivotTransform.position;
                            toPlayer.y = 0f;

                            float currentY = _pivotTransform.eulerAngles.y;
                            float targetY = Quaternion.LookRotation(toPlayer).eulerAngles.y + 90f;
                            float newY = Mathf.MoveTowardsAngle(currentY, targetY, rotationSpeed * Time.deltaTime * 0.5f);
                            float angleToRotate = newY - currentY;

                            foreach (Transform rotatingT in _rotationList)
                            {
                                if (rotatingT != null)
                                    rotatingT.RotateAround(pivotPoint, Vector3.up, angleToRotate);

                                //ScienceBirdTweaks.Logger.LogDebug($"Rotating {rotatingT?.name}: Current Rotation = {rotatingT?.rotation.eulerAngles}");
                            }
                        }
                        else
                        {
                            foreach (Transform rotatingT in _rotationList)
                            {
                                if (rotatingT != null)
                                    rotatingT.RotateAround(pivotPoint, Vector3.up.normalized, rotationSpeed * Time.deltaTime);
                                else
                                    ScienceBirdTweaks.Logger.LogWarning($"Rotating sibling is null! Skipping rotation.");
                            }
                        }
                        rotatingLastFrame = true;
                    }
                    else if (awaitingSpin && !rotatingLastFrame)
                    {
                        StartSpinning();
                    }
                    else
                    {
                        rotatingLastFrame = false;
                    }
                }
                else
                {
                    if (rotatingLastFrame || _isRotating)// stop spinning is only manually called by interaction, so this catches when spinning stops due to take-off (and updates boolean and animator accordingly)
                    {
                        StopSpinning();
                    }
                    rotatingLastFrame = false;
                }
            }
        }

        public void SetAnimatorBool(bool on)
        {
            if (interactAnimator == null)
            {
                ShipFloodlightInteractionHandler interactHandler = Object.FindObjectOfType<ShipFloodlightInteractionHandler>();
                if (interactHandler != null)
                {
                    interactAnimator = interactHandler.interactAnimator;
                }
            }
            if (interactAnimator != null)
            {
                interactAnimator.SetBool("on", on);
            }
        }

        public void StartSpinning()
        {
            if (!_canRotate)
            {
                if (!_startOfRoundInstance.inShipPhase)
                {
                    awaitingSpin = true;// this queues up rotation to occur when it's next able to
                }
                return;
            }

            SetAnimatorBool(true);

            _isRotating = true;
            if (!this.enabled)
                this.enabled = true;

            ScienceBirdTweaks.Logger.LogDebug($"Floodlight rotation enabled.");
        }

        public void StopSpinning()
        {
            SetAnimatorBool(false);
            _isRotating = false;
            awaitingSpin = false;

            ScienceBirdTweaks.Logger.LogDebug($"Floodlight rotation disabled.");
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

                    //ScienceBirdTweaks.Logger.LogDebug($"Found parent renderer '{parentRenderer.name}'. Processing its materials...");


                    Material[] materials = parentRenderer.materials;

                    foreach (Material mat in materials)
                    {
                        if (mat == null) continue;

                        if (!mat.IsKeywordEnabled("_EMISSIVE_COLOR_MAP")) continue;

                        //ScienceBirdTweaks.Logger.LogDebug($"---> Found mat on renderer '{parentRenderer.name}'...");

                        Color originalEmissiveColor = mat.GetColor(Shader.PropertyToID("_EmissiveColor"));

                        //ScienceBirdTweaks.Logger.LogDebug($"---> Initial _EmissiveColor : {originalEmissiveColor}");

                        Color dimmedEmissiveColor = new Color(
                            9.026f * dimmingFactor,
                            8.695f * dimmingFactor,
                            7.750f * dimmingFactor,
                            12.05f * dimmingFactor);

                        //ScienceBirdTweaks.Logger.LogDebug($"---> Dimmed _EmissiveColor : {dimmedEmissiveColor}");

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

            awaitingSpin = false;// requests to start rotation shouldnt carry over to the next day

            if (ScienceBirdTweaks.FloodlightRotation.Value)// Reset rotations & positions once in orbit to prevent drift
            {
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
                        //ScienceBirdTweaks.Logger.LogDebug($"Reset {t.name} to Pos={t.localPosition}, Rot={t.localRotation.eulerAngles}");
                    }
                    else
                    {
                        ScienceBirdTweaks.Logger.LogWarning($"Attempted to reset rotation on a null transform (originally stored).");
                    }
                }
                ScienceBirdTweaks.Logger.LogDebug("Floodlight rotation reset complete.");
            }
            else
            {
                ScienceBirdTweaks.Logger.LogDebug("Floodlight rotation disabled in config, skipping rotation reset.");
            }
            
        }

        public static Vector3 GetClosestPlayerPosition(Transform _pivotTransform) // maybe this and enemy func should be merged $idk
        {
            float minDistance = float.MaxValue;
            Vector3 closestPos = Vector3.zero;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled || player.isPlayerDead || player.isInHangarShipRoom)
                    continue;

                float dist = Vector3.Distance(_pivotTransform.position, player.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPos = player.transform.position;
                }
            }

            //ScienceBirdTweaks.Logger.LogDebug($"Closest player position: {closestPos}");

            return closestPos;
        }

        public static Vector3 GetClosestEnemyPosition(Transform _pivotTransform)
        {
            float closestDist = float.MaxValue;
            Vector3 closestPos = Vector3.zero;

            List<string> enemyBlacklist = new List<string>(); // should pull from class variables, also 90% sure these enemy names arn't correct in-game
            enemyBlacklist.Add("Manticoil");
            enemyBlacklist.Add("Roaming_Locust");
            enemyBlacklist.Add("Tulip_Snake");

            foreach (var enemy in Object.FindObjectsOfType<EnemyAI>())
            {
                if (enemy == null || enemy.isEnemyDead || !enemy.isOutside) // havn't confirmed isOutside does anything
                    continue;

                string enemyName = enemy.enemyType.enemyName;
                if (!enemyBlacklist.Contains(enemyName))
                    continue;

                float dist = Vector3.Distance(_pivotTransform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPos = enemy.transform.position;
                }
            }

            return closestPos;
        }
    }
}