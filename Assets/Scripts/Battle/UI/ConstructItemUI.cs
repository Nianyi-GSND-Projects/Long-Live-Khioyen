using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class ConstructionItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public TMP_Text nameText;
        public Image iconImage; // 按钮自己的图标

        private FacilityDefinition _facility;

        public void Setup(FacilityDefinition fac, System.Action onClick)
        {
            _facility = fac;
            if (nameText != null) nameText.text = fac.unitName;
            if (iconImage != null) iconImage.sprite = fac.icon; // 假设 FacilityDefinition 有 icon

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => 
                {
                    if (GeneralTooltipUI.Instance != null) GeneralTooltipUI.Instance.Hide();
                    onClick?.Invoke();
                });
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_facility != null && GeneralTooltipUI.Instance != null)
            {
                GeneralTooltipUI.Instance.Show(_facility.description, _facility.icon); // 假设有 icon
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (GeneralTooltipUI.Instance != null)
            {
                GeneralTooltipUI.Instance.Hide();
            }
        }
    }
}