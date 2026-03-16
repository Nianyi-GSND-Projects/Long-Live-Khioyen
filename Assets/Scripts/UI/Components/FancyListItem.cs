using Nianyi.UnityPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class FancyListItem : MonoBehaviour
	{
		[SerializeField] GameObject iconContainer;
		[SerializeField] Image iconImage;
		[SerializeField] TMP_Text quantityText;
		[SerializeField] TMP_Text nameText;
		[SerializeField] LayoutGroup infoLayoutGroup;
		[SerializeField] Button button;

		public System.Action onClick;

		protected void Start()
		{
			button.onClick.AddListener(() => onClick?.Invoke());
		}

		#region 实例化
		const string prefabPath = "Prefabs/UI/Common/Fancy List Item";
		public static FancyListItem Instantiate()
		{
			return HierarchyUtility.InstantiatePrefabFromResource<FancyListItem>(prefabPath);
		}

		public static FancyListItem Instantiate(Transform parent)
		{
			var instance = Instantiate();
			instance.transform.SetParent(parent, false);
			return instance;
		}
		#endregion

		#region 外观与行为
		#region ID
		public bool ShowIcon
		{
			get => iconContainer.activeSelf;
			set => iconContainer.SetActive(value);
		}

		public Sprite IconSprite
		{
			get => iconImage.sprite;
			set => iconImage.sprite = value;
		}

		public string ItemName
		{
			get => nameText.text;
			set => nameText.text = value;
		}

		public bool ShowQuantity
		{
			get => quantityText.gameObject.activeSelf;
			set => quantityText.gameObject.SetActive(value);
		}

		public int Quantity
		{
			set => quantityText.text = value.ToString();
		}
		#endregion

		#region 花费
		public void SetCosts(IEnumerable<ResourceDescriptor> costs)
		{
			infoLayoutGroup.transform.ClearChildren();

			foreach(var cost in costs)
			{
				if(cost.quantity == 0)
					continue;

				var field = EconomyField.Instantiate();
				field.transform.SetParent(infoLayoutGroup.transform, false);

				field.SetResourceType(cost);
				field.ValueFloat = cost.quantity;
			}

			infoLayoutGroup.CalculateLayoutInputVertical();
		}

		public void SetCosts(params ResourceDescriptor[] costs) => SetCosts(costs as IEnumerable<ResourceDescriptor>);
		#endregion

		#region 交互
		public bool Interactable
		{
			get => button.interactable;
			set => button.interactable = value;
		}
		#endregion

		#region 特定类型的辅助方法
		public void ApplyItem(ItemDefinition item)
		{
			ItemName = item.name;
			SetCosts(item.costs);
		}

		public void ApplyCommander(GameCommander commander)
		{
			Interactable = commander != null;
			ItemName = commander?.commanderName ?? string.Empty;
			IconSprite = commander?.portrait;
			SetCosts();
		}
		#endregion
		#endregion
	}
}
