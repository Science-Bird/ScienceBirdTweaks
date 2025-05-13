using Unity.Netcode;
using ScienceBirdTweaks.Patches;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace ScienceBirdTweaks.Scripts
{
    public class ShotgunScript : NetworkBehaviour
    {
        public ShotgunItem shotgun;
        public bool holdingDown = false;
        public float startTime;

        private bool sendingRPC1 = false;
        private bool sendingRPC2 = false;

        public float fill = 0f;

        public void Start()
        {
            shotgun = gameObject.GetComponent<ShotgunItem>();
        }

        public void StartHolding(ShotgunItem shotgunItem, bool doEject, bool doAnimation)// initialize local hold event
        {
            if (shotgun == null)
            {
                shotgun = shotgunItem;
            }
            if (ShotgunPatches.HasValidHolder(shotgun))
            {
                holdingDown = doEject;
                startTime = Time.realtimeSinceStartup;
                sendingRPC1 = true;
                StartCoroutine(OpenShotgunAnimation(doEject, doAnimation));
                StartAnimationServerRpc(doEject, doAnimation);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartAnimationServerRpc(bool doEject, bool doAnimation)
        {
            StartAnimationClientRpc(doEject, doAnimation);
        }

        [ClientRpc]
        public void StartAnimationClientRpc(bool doEject, bool doAnimation)
        {
            if (sendingRPC1)
            {
                sendingRPC1 = false;
            }
            else
            {
                fill = 0f;
                StartCoroutine(OpenShotgunAnimation(doEject, doAnimation));
            }
        }

        // All credit for this goes to Axd1x8a / FeeeeK! They're the original creator of LCAmmoCheck, which this is ported from
        public IEnumerator OpenShotgunAnimation(bool doEject, bool doAnimation)// this strategically slows down the reload animation and transitions back to essentially create a new animation
        {
            shotgun.isReloading = true;
            if (doAnimation)
            {
                if (doEject && GameNetworkManager.Instance.localPlayerController != null && shotgun.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    HUDManager.Instance.ChangeControlTip(2, "Hold to eject : [E]");
                }
                shotgun.shotgunShellLeft.enabled = shotgun.shellsLoaded > 0;
                shotgun.shotgunShellRight.enabled = shotgun.shellsLoaded > 1;

                shotgun.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: true);
            }

            yield return new WaitForSeconds(0.3f);

            if (doAnimation)
            {
                shotgun.gunAudio.PlayOneShot(ShotgunPatches.inspectSFX);
                shotgun.gunAnimator.SetBool("Reloading", value: true);
            }

            yield return new WaitForSeconds(0.45f);

            if (doAnimation)
            {
                shotgun.playerHeldBy.playerBodyAnimator.speed = 0.2f;
            }

            yield return new WaitForSeconds(0.55f);

            if (doEject && holdingDown && ShotgunPatches.HasValidHolder(shotgun) && ShotgunPatches.LocalPlayerNotInteracting(shotgun) && ShotgunPatches.AllowedToEject(shotgun))
            {
                fill = 0f;
                holdingDown = false;
                sendingRPC2 = true;
                EjectShells();
                EjectShellsServerRpc();
                if (!doAnimation)
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.4f);

            if (doAnimation)
            {
                shotgun.playerHeldBy.playerBodyAnimator.speed = 0.6f;
                shotgun.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: false);
                shotgun.gunAnimator.SetBool("Reloading", value: false);
            }
            ShotgunTooltipPatches.TooltipUpdate(shotgun);

            yield return new WaitForSeconds(0.3f);

            if (doAnimation)
            {
                shotgun.playerHeldBy.playerBodyAnimator.speed = 1f;
            }

            yield return new WaitForSeconds(0.25f);

            if (doAnimation)
            {
                shotgun.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", false);
            }
            shotgun.isReloading = false;
            yield break;
        }

        public void Update()// make sure button is held down
        {
            if (!ShotgunPatches.unloadEnabled) { return; }
            
            if (fill > 0.1f)
            {
                HUDManager.Instance.holdInteractionCanvasGroup.alpha = Mathf.Lerp(HUDManager.Instance.holdInteractionCanvasGroup.alpha, 1f, 20f * Time.deltaTime);
            }

            if (holdingDown && ShotgunPatches.HasValidHolder(shotgun))
            {
                if (ShotgunPatches.LocalPlayerNotInteracting(shotgun))
                {
                    if (!IngamePlayerSettings.Instance.playerInput.actions.FindAction("ItemTertiaryUse").IsPressed())
                    {
                        if (!ShotgunPatches.ammoCheck)
                        {
                            base.StopAllCoroutines();
                            shotgun.isReloading = false;
                        }
                        //ScienceBirdTweaks.Logger.LogDebug($"STOPPING HOLD DUE TO BUTTON RELEASE");
                        fill = 0f;
                        holdingDown = false;
                    }
                    else
                    {
                        fill += Time.deltaTime;
                        HUDManager.Instance.holdInteractionFillAmount.fillAmount = fill / 1.3f;
                    }
                }
                else
                {
                    if (!ShotgunPatches.ammoCheck)
                    {
                        base.StopAllCoroutines();
                        shotgun.isReloading = false;
                    }
                    //ScienceBirdTweaks.Logger.LogDebug($"STOPPING HOLD DUE TO INTERACTION");
                    fill = 0f;
                    holdingDown = false;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EjectShellsServerRpc()
        {
            EjectShellsClientRpc();
        }

        [ClientRpc]
        public void EjectShellsClientRpc()
        {
            if (sendingRPC2)
            {
                sendingRPC2 = false;
            }
            else
            {
                EjectShells();
            }
        }

        public void EjectShells()// this method is only reached if the local client gets through the above functions
        {
            if (ScienceBirdTweaks.ShotgunMasterDisable.Value || !ShotgunPatches.HasValidHolder(shotgun)) { return; }

            shotgun.shotgunShellLeft.enabled = false;
            shotgun.shotgunShellRight.enabled = false;

            if (!ShotgunPatches.shellRegistered)
            {
                if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(ShotgunPatches.shellPrefab))
                {
                    NutcrackerEnemyAI nutcracker = Resources.FindObjectsOfTypeAll<NutcrackerEnemyAI>().First();
                    if (nutcracker != null)
                    {
                        ShotgunPatches.shellPrefab = nutcracker.shotgunShellPrefab;
                        ScienceBirdTweaks.Logger.LogDebug("Re-finding shell prefab on interact...");
                        if (ShotgunPatches.shellPrefab.GetComponent<NetworkObject>().PrefabIdHash == 0)
                        {
                            ScienceBirdTweaks.Logger.LogError("Shell not registered on interaction due to an incompatibility. To avoid client de-syncs, enable the 'force register shells' config option. Please report this issue!");
                        }
                    }
                }
                else
                {
                    ShotgunPatches.shellRegistered = true;
                }
            }

            //ScienceBirdTweaks.Logger.LogDebug($"Eject called!");

            if (base.IsServer)
            {
                // in short: put it in the ship if it's in orbit, otherwise put it in the current round's scrap container
                Transform parent = ((((!(shotgun.playerHeldBy != null) || !shotgun.playerHeldBy.isInElevator) && !StartOfRound.Instance.inShipPhase) || !(RoundManager.Instance.spawnedScrapContainer != null)) ? StartOfRound.Instance.elevatorTransform : RoundManager.Instance.spawnedScrapContainer);
                for (int i = 0; i < shotgun.shellsLoaded; i++)
                {
                    GameObject obj = Object.Instantiate(ShotgunPatches.shellPrefab, shotgun.playerHeldBy.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.1f, Random.Range(-0.5f, 0.5f)), Quaternion.identity, parent);
                    obj.GetComponent<NetworkObject>().Spawn();
                    GrabbableObject grabbable = obj.GetComponent<NetworkObject>().GetComponent<GrabbableObject>();
                    grabbable.startFallingPosition = obj.transform.position;
                    grabbable.fallTime = 0f;
                    grabbable.hasHitGround = false;
                    grabbable.reachedFloorTarget = false;
                    if (shotgun.playerHeldBy != null && shotgun.playerHeldBy.isInHangarShipRoom)
                    {
                        shotgun.playerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, grabbable);
                    }
                    else
                    {
                        grabbable.isInFactory = shotgun.gameObject.GetComponent<GrabbableObject>().isInFactory;
                    }
                }
            }
            shotgun.shellsLoaded = 0;
        }
    }
}
