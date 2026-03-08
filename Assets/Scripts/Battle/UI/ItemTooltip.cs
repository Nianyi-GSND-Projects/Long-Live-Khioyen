// Assets/Scripts/Battle/UI/ItemTooltip.cs

using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
    public class ItemTooltip : MonoBehaviour
    {
        public TextMeshProUGUI descriptionText;
        public RectTransform rectTransform;

        private void Awake()
        {
            gameObject.SetActive(false);
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        }

        public void Show(ItemDefinition item, Vector3 position)
        {
            descriptionText.text = item.description;
            gameObject.SetActive(true);
            
            // 简单的位置跟随，可以根据需要优化（例如防止超出屏幕）
            transform.position = position + new Vector3(20, -20, 0); 
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}