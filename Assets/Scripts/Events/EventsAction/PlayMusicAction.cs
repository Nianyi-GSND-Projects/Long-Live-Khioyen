using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Actions/Play Music")]
    public class PlayMusicAction : GameEventAction
    {
        public AudioClip musicClip;
        public float fadeDuration = 1.0f;
        public bool loop = true;

        public override void Execute()
        {

            var audioSource = FindObjectOfType<AudioSource>();
            if (audioSource != null)
            {
                audioSource.clip = musicClip;
                audioSource.loop = loop;
                audioSource.Play();
            }
        }
        
        public override IEnumerator ExecuteCoroutine()
        {
            Execute();
            if (fadeDuration > 0) yield return new WaitForSeconds(fadeDuration);
        }
    }
}