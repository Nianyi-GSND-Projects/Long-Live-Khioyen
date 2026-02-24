using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class ActionButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public Button button;
        public TMP_Text buttonText;

        private string _description;

        public void Setup(string text, string description, System.Action onClick, bool interactable)
        {
            if (buttonText != null) buttonText.text = text;
            _description = description;

            if (button != null)
            {
                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick.Invoke());
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_description) && ActionTooltipUI.Instance != null)
            {
                ActionTooltipUI.Instance.Show(_description);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ActionTooltipUI.Instance != null)
            {
                ActionTooltipUI.Instance.Hide();
            }
        }
    }
}