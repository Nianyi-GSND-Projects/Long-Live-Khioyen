using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Actions/Hide CG")]
    public class HideCGAction : GameEventAction
    {
        public float fadeDuration = 0.5f;

        public override void Execute()
        {
            if (EventCGUI.Instance != null)
                Battle.Instance.StartCoroutine(ExecuteCoroutine());
        }

        public override IEnumerator ExecuteCoroutine()
        {
            if (EventCGUI.Instance != null)
            {
                yield return EventCGUI.Instance.HideCG(fadeDuration);
            }
        }
    }
}