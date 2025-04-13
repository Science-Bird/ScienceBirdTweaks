using Unity.Netcode;
using LethalLevelLoader;
using UnityEngine;

namespace ScienceBirdTweaks.Scripts
{
    public class MineAudio : MonoBehaviour
    {
        public AudioClip beepClip;

        public AudioSource audioSource;

        public void PlayBeepAudio()
        {
            audioSource.clip = beepClip;
            audioSource.Play();
            WalkieTalkie.TransmitOneShotAudio(audioSource, beepClip);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
        }
    }
}
