// Assets/Scripts/Battle/UI/LootItemEntry.cs

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
        private ItemTooltip _tooltip; // 引用 Tooltip 控制器

        public void Initialize(inBattleItem item, ItemTooltip tooltip)
        {
            _itemDef = item.definition;
            _tooltip = tooltip;

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
            if (_tooltip != null && _itemDef != null)
            {
                _tooltip.Show(_itemDef, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }
    }
}