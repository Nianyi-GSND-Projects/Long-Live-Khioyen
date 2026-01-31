using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class EconomyField : MonoBehaviour
	{
		[SerializeField] Image iconImage;
		[SerializeField] TMP_Text valueText;

		public Sprite IconSprite
		{
			get => iconImage.sprite;
			set => iconImage.sprite = value;
		}

		public string ValueText
		{
			get => valueText.text;
			set => valueText.text = value;
		}

		public int ValueInt
		{
			set => ValueText = value.ToString();
		}

		public float ValueFloat
		{
			set => ValueText = value.ToString();
		}

		const string prefabPath = "Prefabs/UI/Common/Economy Field";
		public static EconomyField Instantiate()
		{
			return HierarchyUtility.InstantiatePrefabFromResource<EconomyField>(prefabPath);
		}

		const string iconResourcePathPrefix = "Textures/UI/Icons/";
		public void UseIconFromResource(string name)
		{
			string path = iconResourcePathPrefix + name;
			var sprite = Resources.Load<Sprite>(path);
			if(sprite == null)
			{
				Debug.LogWarning($"Failed to load icon from {path} for the icon of an economy field.", this);
				return;
			}
			IconSprite = sprite;
		}

		/// <summary>
		/// 为预定义的几种资源类型设置的方便接口。
		/// </summary>
		public void SetResourceType(EconomyType type)
		{
			switch(type)
			{
				case EconomyType.Food:
					UseIconFromResource("Food");
					break;
				case EconomyType.Material:
					UseIconFromResource("Material");
					break;
				case EconomyType.Money:
					UseIconFromResource("Money");
					break;
				default:
					throw new System.NotSupportedException($"{GetType().Name}.{nameof(SetResourceType)} only supports predefined types.");
			}
		}
	}
}
