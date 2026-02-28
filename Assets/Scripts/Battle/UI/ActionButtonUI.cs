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
        private Sprite _icon;
        
        public void Setup(string text, string description,Sprite icon, System.Action onClick, bool interactable)
        {
            if (buttonText != null) buttonText.text = text;
            _description = description;
            _icon = icon;

            if (button != null)
            {
                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        if (GeneralTooltipUI.Instance != null)
                        {
                            GeneralTooltipUI.Instance.Hide();
                        }
                        onClick.Invoke();
                    });
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_description) && GeneralTooltipUI.Instance != null)
            {
                GeneralTooltipUI.Instance.Show(_description,_icon);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("Hide tooltip");
            if (GeneralTooltipUI.Instance != null)
            {
                GeneralTooltipUI.Instance.Hide();
            }
        }
    }
}