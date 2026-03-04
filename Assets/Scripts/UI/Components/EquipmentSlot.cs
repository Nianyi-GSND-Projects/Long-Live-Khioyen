using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class EquipmentSlot : MonoBehaviour
	{
		[SerializeField] TMP_Dropdown dropdown;
		[SerializeField] Image selectedIcon;

		public Sprite DisplaySprite
		{
			get => selectedIcon.sprite;
			set => selectedIcon.sprite = value;
		}

		#region 生命周期
		protected void Start()
		{
			dropdown.onValueChanged.AddListener(OnOptionSelected);
		}

		protected void OnEnable()
		{
			SetEnabled(true);
		}

		protected void OnDisable()
		{
			SetEnabled(false);
		}

		void SetEnabled(bool value)
		{
			dropdown.gameObject.SetActive(value);
		}
		#endregion

		#region 选项
		public System.Action<EquipmentDefinition> onSelected;
		int selectedIndex = -1;

		readonly List<EquipmentDefinition> options = new();
		public IEnumerable<EquipmentDefinition> Options
		{
			get => options;
			set
			{
				options.Clear();
				options.AddRange(value);

				dropdown.ClearOptions();
				dropdown.AddOptions(new List<TMP_Dropdown.OptionData>() { new() { text = "-", } });
				dropdown.AddOptions(options.Select(d => new TMP_Dropdown.OptionData()
				{
					text = d.name,
					image = d.icon,
				}).ToList());
			}
		}

		public EquipmentDefinition SelectedEquipment => selectedIndex < 0 ? null : options[selectedIndex];

		void OnOptionSelected(int i)
		{
			selectedIndex = i - 1;

			DisplaySprite = SelectedEquipment?.icon;
			onSelected?.Invoke(SelectedEquipment);
		}
		#endregion
	}
}
