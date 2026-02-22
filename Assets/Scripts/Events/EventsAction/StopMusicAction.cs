using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Actions/Stop Music")]
    public class StopMusicAction : GameEventAction
    {
        public float fadeDuration = 1.0f;

        public override void Execute()
        {
            var audioSource = FindObjectOfType<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
        
        public override IEnumerator ExecuteCoroutine()
        {
            Execute(); // 如果支持淡出，这里应该调用淡出协程
            if (fadeDuration > 0) yield return new WaitForSeconds(fadeDuration);
        }
    }
}