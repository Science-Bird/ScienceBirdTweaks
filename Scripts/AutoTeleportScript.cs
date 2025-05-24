using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.Netcode;
using ScienceBirdTweaks.Patches;
using System.Collections.Generic;
using System.Linq;

namespace ScienceBirdTweaks.Scripts
{
    public class AutoTeleportScript : NetworkBehaviour
    {
        private bool doingRoutine = false;

        public static ShipTeleporter teleporter;

        public int currentPlayer = -1;

        public List<int> playerQueue = new List<int>();

        public void StartTeleportRoutine(ShipTeleporter shipTeleporter, int player)
        {
            teleporter = shipTeleporter;
            if (!base.IsServer) { return; }
            if (!doingRoutine)
            {
                if (StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy == null || !StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy.isActiveAndEnabled)
                {
                    StartCoroutine(WaitBeforeTeleport(player));
                }
            }
            else if (currentPlayer != -1 && player != currentPlayer && !playerQueue.Contains(player))
            {
                playerQueue.Add(player);
            }
        }

        [ClientRpc]
        public void DoTeleportRoutineClientRpc(int player)
        {
            if (teleporter == null)
            {
                ShipTeleporter[] teleporters = Object.FindObjectsOfType<ShipTeleporter>().Where(x => !x.isInverseTeleporter).ToArray();
                if (teleporters.Length > 0)
                {
                    teleporter = teleporters.First();
                }
            }
            StartCoroutine(TeleportBodyToShip(player));
        }

        private void AfterTeleport()
        {
            if (!base.IsServer) { return; }
            if (playerQueue.Count > 0)
            {
                int nextPlayer = playerQueue.Last();
                StartTeleportRoutine(teleporter, nextPlayer);
                playerQueue.Remove(nextPlayer);
            }
        }

        private IEnumerator WaitBeforeTeleport(int player)
        {
            yield return new WaitForSeconds(3f);
            if (StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy == null || !StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy.isActiveAndEnabled)
            {
                DoTeleportRoutineClientRpc(player);
            }
            else
            {
                ScienceBirdTweaks.Logger.LogDebug("Found enemy!");
            }
        }

        private IEnumerator TeleportBodyToShip(int player)// recreation of vanilla teleport routine, but only containing what's needed for dead bodies
        {
            doingRoutine = true;
            currentPlayer = player;
            PlayerControllerB playerToBeamUp = StartOfRound.Instance.allPlayerScripts[player];
            teleporter.teleporterAnimator.SetTrigger("useTeleporter");
            teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterSpinSFX);
            if (playerToBeamUp == null)
            {
                ScienceBirdTweaks.Logger.LogDebug("Targeted player is null");
                yield break;
            }
            if (playerToBeamUp.deadBody != null)
            {
                if (playerToBeamUp.deadBody.beamUpParticle == null)
                {
                    yield break;
                }
                playerToBeamUp.deadBody.beamUpParticle.Play();
                playerToBeamUp.deadBody.bodyAudio.PlayOneShot(teleporter.beamUpPlayerBodySFX);
            }
            //ScienceBirdTweaks.Logger.LogDebug("Teleport A");
            yield return new WaitForSeconds(3f);
            bool flag = false;
            if (playerToBeamUp.deadBody != null)
            {
                if (playerToBeamUp.deadBody.grabBodyObject == null || !playerToBeamUp.deadBody.grabBodyObject.isHeldByEnemy)
                {
                    flag = true;
                    playerToBeamUp.deadBody.attachedTo = null;
                    playerToBeamUp.deadBody.attachedLimb = null;
                    playerToBeamUp.deadBody.secondaryAttachedLimb = null;
                    playerToBeamUp.deadBody.secondaryAttachedTo = null;
                    playerToBeamUp.deadBody.SetRagdollPositionSafely(teleporter.teleporterPosition.position, disableSpecialEffects: true);
                    playerToBeamUp.deadBody.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
                    if (playerToBeamUp.deadBody.grabBodyObject != null)// this part ensures the body actually gets collected without needing to be picked up
                    {
                        GrabbableObject bodyGrabbable = playerToBeamUp.deadBody.grabBodyObject;
                        RoundManager.Instance.scrapCollectedInLevel += bodyGrabbable.scrapValue;
                        RoundManager.Instance.CollectNewScrapForThisRound(bodyGrabbable);
                        bodyGrabbable.OnBroughtToShip();
                        StartOfRound.Instance.currentShipItemCount++;
                        if (bodyGrabbable.isHeld && bodyGrabbable.playerHeldBy != null)
                        {
                            bodyGrabbable.playerHeldBy.DropAllHeldItems();
                        }
                    }
                }
            }
            //ScienceBirdTweaks.Logger.LogDebug("Teleport B");
            teleporter.SetPlayerTeleporterId(playerToBeamUp, -1);
            if (flag)
            {
                teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterBeamUpSFX);
                if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
                {
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                }
            }
            if (playerToBeamUp.deadBody == null && playerToBeamUp.isPlayerDead)
            {
                DisplayCustomScrapBox();
            }
            //ScienceBirdTweaks.Logger.LogDebug("Teleport C");
            doingRoutine = false;
            currentPlayer = -1;
            AfterTeleport();
        }


        public void DisplayBoxAfterCheck(int player)
        {
            if (!base.IsServer) { return; }
            if (StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy == null || !StartOfRound.Instance.allPlayerScripts[player].redirectToEnemy.isActiveAndEnabled)
            {
                DisplayCustomScrapBoxClientRpc();
            }
        }

        [ClientRpc]
        public void DisplayCustomScrapBoxClientRpc()
        {
            DisplayCustomScrapBox();
        }

        public void DisplayCustomScrapBox()// recreation of scrap display method, using custom mesh and text
        {
            HUDManager HUD = HUDManager.Instance;

            HUD.UIAudio.PlayOneShot(PlayerDeathPatches.HUDWarning, 0.5f);
            GameObject gameObject = UnityEngine.Object.Instantiate(PlayerDeathPatches.questionMark, HUD.ScrapItemBoxes[HUD.nextBoxIndex].itemObjectContainer);
            gameObject.transform.localPosition = new Vector3(0f, 0f, -1f);
            gameObject.transform.localScale = gameObject.transform.localScale * 3.5f;
            gameObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i].gameObject.layer != 22)
                {
                    Material[] sharedMaterials = componentsInChildren[i].sharedMaterials;
                    componentsInChildren[i].rendererPriority = 70;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        sharedMaterials[j] = HUD.hologramMaterial;
                    }
                    componentsInChildren[i].sharedMaterials = sharedMaterials;
                    componentsInChildren[i].gameObject.layer = 5;
                }
            }
            HUD.ScrapItemBoxes[HUD.nextBoxIndex].itemDisplayAnimator.SetTrigger("collect");
            HUD.ScrapItemBoxes[HUD.nextBoxIndex].headerText.text = "Body unrecoverable!";
            HUD.ScrapItemBoxes[HUD.nextBoxIndex].valueText.text = "";
            if (HUD.boxesDisplaying > 0)
            {
                HUD.ScrapItemBoxes[HUD.nextBoxIndex].UIContainer.anchoredPosition = new Vector2(HUD.ScrapItemBoxes[HUD.nextBoxIndex].UIContainer.anchoredPosition.x, HUD.ScrapItemBoxes[HUD.bottomBoxIndex].UIContainer.anchoredPosition.y - 124f);
            }
            else
            {
                HUD.ScrapItemBoxes[HUD.nextBoxIndex].UIContainer.anchoredPosition = new Vector2(HUD.ScrapItemBoxes[HUD.nextBoxIndex].UIContainer.anchoredPosition.x, HUD.bottomBoxYPosition);
            }
            HUD.bottomBoxIndex = HUD.nextBoxIndex;
            StartCoroutine(HUD.displayScrapTimer(gameObject));
            HUD.playScrapDisplaySFX();
            HUD.boxesDisplaying++;
            HUD.nextBoxIndex = (HUD.nextBoxIndex + 1) % 3;
        }
    }
}
