using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class CostPreviewPanel : MonoBehaviour
	{
		void Awake()
		{
			Description = Description;
		}

		void Update()
		{
			transform.position = Pointer.current.position.value;
		}

		[SerializeField] LayoutGroup costArea;
		[SerializeField] TMP_Text descriptionText;

		public string Description
		{
			get => descriptionText.text;
			set
			{
				descriptionText.text = value;
				descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
			}
		}

		public void UpdateCostData(Economy cost)
		{
			costArea.transform.ClearChildren();
			foreach(var d in cost.ToDescriptors())
			{
				if(d.quantity == 0)
					continue;

				var item = EconomyField.Instantiate();
				item.transform.SetParent(costArea.transform, false);
				item.SetResourceType(d);
				item.ValueFloat = d.quantity;
			}
		}
	}
}