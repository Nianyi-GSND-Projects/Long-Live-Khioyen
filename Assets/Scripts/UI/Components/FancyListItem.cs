using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Nianyi.UnityPack;

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
		public void SetCosts(IEnumerable<CostDescriptor> costs)
		{
			infoLayoutGroup.transform.ClearChildren();

			foreach(var cost in costs)
			{
				var field = EconomyField.Instantiate();
				field.transform.SetParent(infoLayoutGroup.transform, false);

				switch(cost.type)
				{
					case EconomyType.Custom:
						field.IconSprite = cost.customSprite;
						break;
					default:
						field.SetResourceType(cost.type);
						break;
				}
				field.ValueFloat = cost.value;
			}

			infoLayoutGroup.CalculateLayoutInputVertical();
		}

		public void SetCosts(params CostDescriptor[] costs) => SetCosts(costs as IEnumerable<CostDescriptor>);
		#endregion

		#region 交互
		public bool Interactable
		{
			get => button.interactable;
			set => button.interactable = value;
		}
		#endregion
		#endregion

		#region 实例化
		const string prefabPath = "Prefabs/UI/Common/Fancy List Item";
		public static FancyListItem Instantiate()
		{
			return HierarchyUtility.InstantiatePrefabFromResource<FancyListItem>(prefabPath);
		}
		#endregion
	}
}
