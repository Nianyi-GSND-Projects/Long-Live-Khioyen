using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class FancyListItem : MonoBehaviour
	{
		[SerializeField] TMP_Text nameText;
		[SerializeField] LayoutGroup infoLayoutGroup;
		[SerializeField] Button button;

		public string ItemName
		{
			get => nameText.text;
			set => nameText.text = value;
		}

		public struct CostDescriptor
		{
			public EconomyType type;
			/// <summary>
			/// 若此 descriptor 的 <c>type</c> 为 <c>Custom</c>，则此字段值用为图标。
			/// </summary>
			public Sprite customSprite;

			public float value;
		}

		const string prefabPath = "Prefabs/UI/Common/Fancy List Item";
		public static FancyListItem Instantiate()
		{
			return HierarchyUtility.InstantiatePrefabFromResource<FancyListItem>(prefabPath);
		}

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

		public bool Interactable
		{
			get => button.interactable;
			set => button.interactable = value;
		}
	}
}
