using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace ScienceBirdTweaks.ZapGun
{
    public class MineZapper : NetworkBehaviour, IShockableWithGun
    {
        Landmine mine;
        TerminalAccessibleObject terminalObj;
        public float cooldown = 3.2f;
        private float cooldownTimer;
        private float startTime;
        private float effectiveCooldown;
        public Light light1;
        public Light light2;
        public Light indirectLight;
        public bool tempStun = false;
        public bool startRoutine = false;
        public bool masterZappable = true;
        public float multiplier = 0.25f;
        public bool disabled = false;

        private void Start()
        {
            mine = GetComponent<Landmine>();
            terminalObj = GetComponent<TerminalAccessibleObject>();
            Light[] mineLights = GetComponentsInChildren<Light>();
            foreach (Light light in mineLights)
            {
                if (light.gameObject.name == "BrightLight")
                {
                    light1 = light;
                }
                if (light.gameObject.name == "BrightLight2")
                {
                    light2 = light;
                }
                if (light.gameObject.name == "IndirectLight")
                {
                    indirectLight = light;
                }
            }
            Animator mineAnimator = mine.gameObject.GetComponent<Animator>();
            if (mineAnimator == null)
            {
                disabled = true;
                return;
            }
            mineAnimator.runtimeAnimatorController = HazardPatches.newController;
            masterZappable = ScienceBirdTweaks.ZappableMines.Value && ScienceBirdTweaks.ZapGunRework.Value;
            cooldown = ScienceBirdTweaks.MineZapBaseCooldown.Value;
            multiplier = ScienceBirdTweaks.ZapScalingFactor.Value;
        }

        bool IShockableWithGun.CanBeShocked()
        {
            return !mine.hasExploded && !terminalObj.inCooldown && masterZappable && !disabled;
        }

        float IShockableWithGun.GetDifficultyMultiplier()
        {
            return 0.5f;
        }

        NetworkObject IShockableWithGun.GetNetworkObject()
        {
            return NetworkObject;
        }

        Vector3 IShockableWithGun.GetShockablePosition()
        {
            return gameObject.transform.position + new Vector3(0, 0.5f, 0);
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
            light1.intensity = 0f;
            light2.intensity = 0f;
            indirectLight.intensity = 0f;
            tempStun = true;
            startTime = Time.realtimeSinceStartup;
            mine.ToggleMineEnabledLocalClient(false);
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
            mine.mineActivated = true;
            mine.ToggleMineEnabledLocalClient(false);
            //terminalObj.terminalCodeEvent.Invoke(GameNetworkManager.Instance.localPlayerController);
            effectiveCooldown = cooldown * (elapsedTime * multiplier);
            cooldownTimer = effectiveCooldown;
            ScienceBirdTweaks.Logger.LogDebug($"Freezing for {cooldownTimer}s ({elapsedTime * multiplier}x normal)");
            StartCoroutine(mineCoolDown());
        }

        private IEnumerator mineCoolDown()
        {
            while (!startRoutine)
            {
                yield return null;
            }
            startRoutine = false;
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
