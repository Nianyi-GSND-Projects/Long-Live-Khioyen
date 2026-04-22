using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
    [Serializable]
    public class TurnBannerConfig
    {
        [Tooltip("横幅显示的文字")]
        public string text = "回合开始";
        [Tooltip("文字颜色")]
        public Color textColor = Color.white;
    }

    public class TurnBannerUI : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI turnText;

        [Header("Animation Settings")]
        public float fadeInDuration = 0.3f;
        public float displayDuration = 1.0f;
        public float fadeOutDuration = 0.5f;

        [Header("Faction Settings")]
        public TurnBannerConfig playerConfig;
        public TurnBannerConfig enemyConfig;

        public void Show(Faction faction, int turnCount)
        {
            if (canvasGroup == null || turnText == null) return;

            TurnBannerConfig config = GetConfig(faction);
            if (config == null) return;

            gameObject.SetActive(true);

            string displayText = faction == Faction.Player
                ? $"{config.text} - Turn {turnCount}"
                : config.text;

            turnText.text = displayText;
            turnText.color = config.textColor;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            StopAllCoroutines();
            StartCoroutine(BannerRoutine());
        }

        private TurnBannerConfig GetConfig(Faction faction)
        {
            switch (faction)
            {
                case Faction.Player:
                    return playerConfig;
                case Faction.Enemy:
                    return enemyConfig;
                default:
                    return null;
            }
        }

        private IEnumerator BannerRoutine()
        {
            yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
