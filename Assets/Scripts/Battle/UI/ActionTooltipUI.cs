using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
    public class ActionTooltipUI : MonoBehaviour
    {
        public static ActionTooltipUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject panel;
        public TMP_Text descriptionText;
        public CanvasGroup canvasGroup;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        public void Show(string text)
        {
            if (panel != null) panel.SetActive(true);
            if (descriptionText != null) descriptionText.text = text;
            if (canvasGroup != null) canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }
    }
}