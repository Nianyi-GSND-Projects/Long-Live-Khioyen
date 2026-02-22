using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LongLiveKhioyen
{
    public class EventCGUI : MonoBehaviour
    {
        public static EventCGUI Instance { get; private set; }

        [Header("UI References")]
        public Image cgImage;
        public CanvasGroup canvasGroup; // 用于淡入淡出

        private void Awake()
        {
            Instance = this;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                cgImage.gameObject.SetActive(false);
            }
        }

        public IEnumerator ShowCG(Sprite sprite, float duration)
        {
            if (cgImage == null || canvasGroup == null) yield break;

            cgImage.sprite = sprite;
            cgImage.gameObject.SetActive(true);

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.1f, duration);
                canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        public IEnumerator HideCG(float duration)
        {
            if (cgImage == null || canvasGroup == null) yield break;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.1f, duration);
                canvasGroup.alpha = Mathf.Lerp(1, 0, t);
                yield return null;
            }
            canvasGroup.alpha = 0;
            cgImage.gameObject.SetActive(false);
        }
    }
}