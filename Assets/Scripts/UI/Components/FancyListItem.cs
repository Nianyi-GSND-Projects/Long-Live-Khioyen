using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class FancyListItem : MonoBehaviour
	{
		[SerializeField] TMP_Text nameText;
		[SerializeField] LayoutGroup infoLayoutGroup;

		protected void Start()
		{
			// DEBUG
			infoLayoutGroup.transform.ClearChildren();
			for(int i = 0; i < 2; ++i)
			{
				var field = EconomyField.Instantiate();
				field.transform.SetParent(infoLayoutGroup.transform, false);
				field.ValueInt = i * 100;
				field.SetResourceType(EconomyType.Food);
			}
			infoLayoutGroup.CalculateLayoutInputVertical();
		}
	}
}
