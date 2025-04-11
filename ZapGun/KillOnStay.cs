using GameNetcodeStuff;
using UnityEngine;

namespace ScienceBirdTweaks.ZapGun
{
    public class KillOnStay : MonoBehaviour
    {
        public bool dontSpawnBody;

        public CauseOfDeath causeOfDeath = CauseOfDeath.Crushing;

        public bool justDamage;

        public int deathAnimation;

        [Space(5f)]

        public int playAudioOnDeath = -1;

        public GameObject spawnPrefab;

        public bool disallowKillingInShip;

        // custom trigger script for door kill trigger

        public void KillPlayer(PlayerControllerB playerWhoTriggered)
        {
            if (justDamage)// currently unused logic leftover from vanilla function
            {
                playerWhoTriggered.DamagePlayer(25);
                return;
            }
            if (playAudioOnDeath != -1)
            {
                SoundManager.Instance.PlayAudio1AtPositionForAllClients(playerWhoTriggered.transform.position, playAudioOnDeath);
            }
            if (spawnPrefab != null)
            {
                Object.Instantiate(spawnPrefab, playerWhoTriggered.lowerSpine.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            }
            playerWhoTriggered.KillPlayer(Vector3.zero, !dontSpawnBody, causeOfDeath, deathAnimation);
        }

        private void OnTriggerStay(Collider other)
        {
            if (ScienceBirdTweaks.PlayerLethalBigDoors.Value &&  other.gameObject.CompareTag("Player") && (bool)other.gameObject.GetComponent<PlayerControllerB>() && other.gameObject.GetComponent<PlayerControllerB>().IsOwner && !other.gameObject.GetComponent<PlayerControllerB>().isPlayerDead)
            {
                KillPlayer(other.gameObject.GetComponent<PlayerControllerB>());
            }
            if (ScienceBirdTweaks.EnemyLethalBigDoors.Value && RoundManager.Instance.IsServer && other.CompareTag("Enemy"))
            {
                EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                if (component != null && !component.mainScript.isEnemyDead)
                {
                    component.mainScript.KillEnemyOnOwnerClient();
                }
            }
        }
    }
}
