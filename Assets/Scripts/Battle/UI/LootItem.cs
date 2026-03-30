using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace LongLiveKhioyen
{
    public class LootItem: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI amountText;
        public Image background;
        
        private ItemDefinition _itemDef;
        private GameObject _tooltipPrefab;
        private GameObject _currentTooltipInstance; // 记录当前生成的 Tooltip 实例

        public void Initialize(inBattleItem item, GameObject tooltipPrefab)
        {
            _itemDef = item.definition;
            _tooltipPrefab = tooltipPrefab;

            if (_itemDef != null)
            {
                iconImage.sprite = _itemDef.icon;
                nameText.text = _itemDef.itemName;
                amountText.text = $"x{item.amount}";
                if (background != null && BattleParam.Instance != null)
                {
                    background.color = BattleParam.Instance.GetRarityColor(_itemDef.rarity);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipPrefab != null && _itemDef != null && _currentTooltipInstance == null)
            {
                // 获取最顶层的 Canvas，防止 Tooltip 被 ScrollView 裁切
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                Transform spawnParent = parentCanvas != null ? parentCanvas.transform : transform;

                // 生成实例
                _currentTooltipInstance = Instantiate(_tooltipPrefab, spawnParent);
                
                ItemTooltip tooltipComp = _currentTooltipInstance.GetComponent<ItemTooltip>();
                if (tooltipComp != null)
                {
                    tooltipComp.Show(_itemDef, transform.position);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 鼠标移出时销毁实例
            if (_currentTooltipInstance != null)
            {
                Destroy(_currentTooltipInstance);
                _currentTooltipInstance = null;
            }
        }
        
        // 保底措施：如果这个 LootItem 在鼠标悬停时被意外销毁（比如关掉了面板），也要清理掉 Tooltip
        private void OnDisable()
        {
            if (_currentTooltipInstance != null)
            {
                Destroy(_currentTooltipInstance);
                _currentTooltipInstance = null;
            }
        }
    }
}