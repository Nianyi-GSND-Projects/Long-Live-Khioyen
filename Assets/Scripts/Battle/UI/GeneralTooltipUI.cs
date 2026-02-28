using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class GeneralTooltipUI : MonoBehaviour
    {
        public static GeneralTooltipUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject panel;
        public TMP_Text descriptionText;
        public Image iconImage; // [新增]
        public CanvasGroup canvasGroup;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        public void Show(string text, Sprite icon)
        {
            if (panel != null) panel.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1;
            if (descriptionText != null) descriptionText.text = text;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }
    }
}