using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace ScienceBirdTweaks.ZapGun
{
    public class SpikesZapper : NetworkBehaviour, IShockableWithGun
    {
        SpikeRoofTrap spikes;
        TerminalAccessibleObject terminalObj;
        public GameObject mainObj;
        public float cooldown = 7f;
        private float cooldownTimer;
        private float startTime;
        private float effectiveCooldown;
        public Light light;
        public GameObject supportLights;
        public bool tempStun = false;
        public Material originalMat;
        public bool startRoutine = false;
        public bool masterZappable = true;
        public float multiplier = 0.25f;

        private void Start()
        {
            mainObj = transform.parent.parent.gameObject;
            supportLights = mainObj.transform.Find("AnimContainer").Find("BaseSupport").gameObject;
            ScienceBirdTweaks.Logger.LogDebug(mainObj.name);
            mainObj.layer = 21;

            light = GetComponent<Light>();
            spikes = mainObj.GetComponentInChildren<SpikeRoofTrap>();
            terminalObj = mainObj.GetComponentInChildren<TerminalAccessibleObject>();
            originalMat = supportLights.GetComponent<MeshRenderer>().materials[0];
            masterZappable = ScienceBirdTweaks.ZappableSpikeTraps.Value && ScienceBirdTweaks.ZapGunRework.Value;
            cooldown = ScienceBirdTweaks.SpikeTrapBaseCooldown.Value;
            multiplier = ScienceBirdTweaks.ZapScalingFactor.Value;
        }

        bool IShockableWithGun.CanBeShocked()
        {
            return spikes.trapActive && !terminalObj.inCooldown && masterZappable;
        }

        float IShockableWithGun.GetDifficultyMultiplier()
        {
            return 0.7f;
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
            if (terminalObj.inCooldown || tempStun)
            {
                return;
            }
            startRoutine = false;
            ScienceBirdTweaks.Logger.LogDebug("SHOCK1");
            startTime = Time.realtimeSinceStartup;
            tempStun = true;
            spikes.ToggleSpikesEnabledLocalClient(false);
            terminalObj.inCooldown = true;
        }

        void IShockableWithGun.StopShockingWithGun()
        {
            if (!terminalObj.inCooldown || !tempStun)
            {
                return;
            }
            tempStun = false;
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            spikes.trapActive = true;
            spikes.ToggleSpikesEnabledLocalClient(false);
            effectiveCooldown = cooldown * (elapsedTime * multiplier);
            cooldownTimer = effectiveCooldown;
            ScienceBirdTweaks.Logger.LogDebug($"Freezing for {cooldownTimer}s ({elapsedTime * multiplier}x normal)");
            StartCoroutine(spikesCoolDown());
        }

        private IEnumerator spikesCoolDown()
        {
            while (!startRoutine)
            {
                yield return null;
            }
            startRoutine = false;
            ScienceBirdTweaks.Logger.LogDebug($"COOLDOWN: {terminalObj.inCooldown}");
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
        }
    }
}
