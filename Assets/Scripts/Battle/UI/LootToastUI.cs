using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace LongLiveKhioyen
{
    public class LootToastUI : MonoBehaviour
    {
        [Header("UI References")]
        // public Image iconImage;
        public TMP_Text messageText;
        public Button closeButton;
        public CanvasGroup canvasGroup;

        [Header("Settings")]
        public float displayDuration = 3.0f;
        public float fadeDuration = 0.5f;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Initialize(string message)
        {
            if (messageText != null) messageText.text = message;

            StartCoroutine(DisplayRoutine());
        }

        private IEnumerator DisplayRoutine()
        {
            // 淡入
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime / fadeDuration;
                    canvasGroup.alpha = t;
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }

            // 等待
            yield return new WaitForSeconds(displayDuration);

            // 关闭
            Close();
        }

        private void Close()
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeOutAndDestroy()
        {
            if (canvasGroup != null)
            {
                float t = 1;
                while (t > 0f)
                {
                    t -= Time.deltaTime / fadeDuration;
                    canvasGroup.alpha = t;
                    yield return null;
                }
            }
            Destroy(gameObject);
        }
    }
}