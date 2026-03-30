using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class ItemTooltip : MonoBehaviour
    {
        public TextMeshProUGUI descriptionText;
        public RectTransform rectTransform;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; 
            cg.interactable = false;
        }

        public void Show(ItemDefinition item, Vector3 mousePosition)
        {
            descriptionText.text = item.description;

            // 1. 强制立刻刷新布局，获取真实的宽高
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            // 2. 计算智能偏移与防穿透
            UpdatePositionWithScreenBounds(mousePosition);
        }

        private void UpdatePositionWithScreenBounds(Vector3 mousePos)
        {
            // 基础偏移量（鼠标右下方）
            float offsetX = 20f;
            float offsetY = -20f;

            // 获取 Tooltip 刷新后的真实宽高
            float width = rectTransform.rect.width * rectTransform.lossyScale.x;
            float height = rectTransform.rect.height * rectTransform.lossyScale.y;

            // 计算预计的右下角坐标
            Vector3 finalPos = mousePos + new Vector3(offsetX, offsetY, 0);

            // 屏幕边界检测 (如果超出了屏幕右侧，就翻转到鼠标左边)
            if (finalPos.x + width > Screen.width)
            {
                finalPos.x = mousePos.x - width - offsetX;
            }

            // 屏幕边界检测 (如果超出了屏幕底部，就翻转到鼠标上边)
            if (finalPos.y - height < 0)
            {
                finalPos.y = mousePos.y + height - offsetY;
            }

            // 最终赋值
            transform.position = finalPos;
        }
    }
}