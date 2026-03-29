using System.Collections;
using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
    public class BattleTitlePanel : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI titleText; // 现在只需要一个文本框

        [Header("Animation Settings")]
        public float fadeDuration = 1.0f;    // 淡入淡出的通用时间
        public float displayDuration = 2.0f; // 每次文字停留的时间

        public void ShowTitle(string zhName, string enName)
        {
            if (titleText == null || canvasGroup == null) return;

            gameObject.SetActive(true);
            
            // 初始化状态：整体全透明，文字本身不透明，禁用交互
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            Color c = titleText.color;
            c.a = 1f;
            titleText.color = c;

            StopAllCoroutines();
            StartCoroutine(TitleAnimationRoutine(zhName, enName));
        }

        private IEnumerator TitleAnimationRoutine(string zhName, string enName)
        {
            // ==========================================
            // 1. 设置为中文，整体 UI 面板淡入
            // ==========================================
            titleText.text = zhName;
            yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

            // ==========================================
            // 2. 中文停留展示
            // ==========================================
            yield return new WaitForSeconds(displayDuration);

            // ==========================================
            // 3. 中英交替：中文单字淡出 -> 换字 -> 英文单字淡入
            // ==========================================
            yield return StartCoroutine(FadeTextAlpha(1f, 0f, fadeDuration));
            
            titleText.text = enName;
            
            yield return StartCoroutine(FadeTextAlpha(0f, 1f, fadeDuration));

            // ==========================================
            // 4. 英文停留展示
            // ==========================================
            yield return new WaitForSeconds(displayDuration);

            // ==========================================
            // 5. 最终退场：整体 UI 面板淡出
            // ==========================================
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));

            // 动画结束，关闭对象
            gameObject.SetActive(false);
        }

        // --- 辅助协程：控制整个面板的透明度 (包括背景图) ---
        private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = endAlpha;
        }

        // --- 辅助协程：仅控制文字本身的透明度 ---
        private IEnumerator FadeTextAlpha(float startAlpha, float endAlpha, float duration)
        {
            Color c = titleText.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                titleText.color = c;
                yield return null;
            }
            c.a = endAlpha;
            titleText.color = c;
        }
    }
}