using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.UI;
using DunGen;
using static UnityEngine.GraphicsBuffer;

namespace ScienceBirdTweaks.ZapGun
{
    public class TurretZapper : NetworkBehaviour, IShockableWithGun
    {
        Turret turret;
        TerminalAccessibleObject terminalObj;
        public bool panicMode = false;
        public float cooldown = 7f;
        private float cooldownTimer;
        private float startTime;
        private float effectiveCooldown;
        public Quaternion savedRot;
        public float savedTimer;
        private bool restoredRot = true;
        public bool masterZappable = true;
        public float multiplier = 0.25f;
        private void Start()
        {
            turret = GetComponent<Turret>();
            terminalObj = GetComponent<TerminalAccessibleObject>();
            ScienceBirdTweaks.Logger.LogDebug(turret);
            masterZappable = ScienceBirdTweaks.ZappableTurrets.Value && ScienceBirdTweaks.ZapGunRework.Value;
            cooldown = ScienceBirdTweaks.TurretZapBaseCooldown.Value;
            multiplier = ScienceBirdTweaks.ZapScalingFactor.Value;
        }

        bool IShockableWithGun.CanBeShocked()
        {
            return turret.turretActive && !panicMode && !terminalObj.inCooldown && masterZappable;
        }

        float IShockableWithGun.GetDifficultyMultiplier()
        {
            return 1f;
        }

        NetworkObject IShockableWithGun.GetNetworkObject()
        {
            return NetworkObject;
        }

        Vector3 IShockableWithGun.GetShockablePosition()
        {
            return gameObject.transform.position;
        }

        Transform IShockableWithGun.GetShockableTransform()
        {
            return gameObject.transform;
        }

        void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
        {
            if (terminalObj.inCooldown || panicMode)
            {
                return;
            }
            if (terminalObj != null)
            {
                startTime = Time.realtimeSinceStartup;
                //turret.turretActive = false;
                if (turret.turretMode != 0)
                {
                    turret.rotatingClockwise = false;
                    turret.SwitchTurretMode(0);
                    turret.turretAnimator.SetInteger("TurretMode", 1);
                    if (turret.fadeBulletAudioCoroutine != null)
                    {
                        StopCoroutine(turret.fadeBulletAudioCoroutine);
                    }
                    turret.fadeBulletAudioCoroutine = StartCoroutine(turret.FadeBulletAudio());
                    turret.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                }
                terminalObj.inCooldown = true;
                turret.mainAudio.Stop();
                turret.farAudio.Stop();
                turret.turretActive = false;
                turret.berserkAudio.Play();
                turret.rotationSpeed = 336f;
                turret.rotatingSmoothly = true;
                turret.wasTargetingPlayerLastFrame = false;
                turret.targetPlayerWithRotation = null;

                savedTimer = turret.switchRotationTimer;
                savedRot = turret.turretRod.rotation;
                panicMode = true;
            }
        }

        void IShockableWithGun.StopShockingWithGun()
        {
            if (!terminalObj.inCooldown || !panicMode)
            {
                return;
            }
            panicMode = false;
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            turret.turretActive = true;
            turret.berserkAudio.Stop();
            restoredRot = false;

            turret.rotationSpeed = 28f;
            //turret.rotatingClockwise = true;
            terminalObj.terminalCodeEvent.Invoke(GameNetworkManager.Instance.localPlayerController);
            effectiveCooldown = cooldown * (elapsedTime * multiplier);
            cooldownTimer = effectiveCooldown;
            ScienceBirdTweaks.Logger.LogDebug($"Freezing for {cooldownTimer}s ({elapsedTime * multiplier}x normal)");
            StartCoroutine(turretCoolDown());
        }

        private void Update()// while turret is in panic mode, simulate update loop so it can rotate
        {
            if (panicMode && terminalObj.inCooldown)
            {
                if (GameNetworkManager.Instance.localPlayerController.IsHost)
                {
                    if (turret.switchRotationTimer >= 7f)
                    {
                        turret.switchRotationTimer = 0f;
                        bool setRotateRight = !turret.rotatingRight;
                        turret.SwitchRotationClientRpc(setRotateRight);
                        turret.SwitchRotationOnInterval(setRotateRight);
                    }
                    else
                    {
                        turret.switchRotationTimer += Time.deltaTime * 12f;
                    }
                }
                if (turret.rotatingClockwise)
                {
                    turret.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, turret.turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
                    turret.turretRod.rotation = Quaternion.RotateTowards(turret.turretRod.rotation, turret.turnTowardsObjectCompass.rotation, turret.rotationSpeed * Time.deltaTime);
                    return;
                }
                if (turret.rotatingSmoothly)
                {
                    turret.turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(turret.targetRotation, 0f - turret.rotationRange, turret.rotationRange), 180f);
                }
                turret.turretRod.rotation = Quaternion.RotateTowards(turret.turretRod.rotation, turret.turnTowardsObjectCompass.rotation, turret.rotationSpeed * Time.deltaTime);
            }
            else if (!restoredRot && terminalObj.inCooldown)// slowly restore rotation to value before the panic sequence, to ensure that the turret's range is relatively unchanged and that things stay synced between clients
            {
                turret.turretRod.rotation = Quaternion.Slerp(turret.turretRod.rotation, savedRot, Time.deltaTime / cooldownTimer);
            }
        }

        private IEnumerator turretCoolDown()
        {
            if (terminalObj != null)
            {
                if (!terminalObj.initializedValues)
                {
                    terminalObj.InitializeValues();
                }
                Image cooldownBar = terminalObj.mapRadarBox;
                Image[] componentsInChildren = terminalObj.mapRadarText.gameObject.GetComponentsInChildren<Image>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    if (componentsInChildren[i].type == Image.Type.Filled)
                    {
                        cooldownBar = componentsInChildren[i];
                    }
                }
                cooldownBar.enabled = true;
                terminalObj.mapRadarText.color = Color.red;
                terminalObj.mapRadarBox.color = Color.red;
                while (cooldownTimer > 0f)
                {
                    yield return null;
                    cooldownTimer -= Time.deltaTime;
                    cooldownBar.fillAmount = cooldownTimer / effectiveCooldown;
                }
                terminalObj.TerminalCodeCooldownReached();
                terminalObj.mapRadarText.color = Color.green;
                terminalObj.mapRadarBox.color = Color.green;
                cooldownTimer = 1.5f;
                int frameNum = 0;
                while (cooldownTimer > 0f)
                {
                    yield return null;
                    cooldownTimer -= Time.deltaTime;
                    cooldownBar.fillAmount = Mathf.Abs(cooldownTimer / 1.5f - 1f);
                    frameNum++;
                    if (frameNum % 7 == 0)
                    {
                        terminalObj.mapRadarText.enabled = !terminalObj.mapRadarText.enabled;
                    }
                }
                terminalObj.mapRadarText.enabled = true;
                cooldownBar.enabled = false;
                terminalObj.inCooldown = false;
                turret.turretRod.rotation = savedRot;
                if (base.IsServer)
                {
                    turret.switchRotationTimer = savedTimer;
                }
                restoredRot = true;
            }
        }

    }
}
