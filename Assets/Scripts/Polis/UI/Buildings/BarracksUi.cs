using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace LongLiveKhioyen
{
	public class BarracksUi : MonoBehaviour
	{
		#region Life cycle
		protected void Start()
		{
			SetupEquipmentSlots();
			SetupBattalionArea();

			PolisData.Current.onGarrisonChanged += RefreshCommanders;
			PolisData.Current.onNextPromotableCommanderChanged += RefreshPromotion;
			PolisData.Current.StockedItems.onChanged += RefreshBattalionArea;

			Refresh();
			InspectCommander();
		}

		protected void OnDestroy()
		{
			SelectedBattalion = null;  // 解绑事件
			PolisData.Current.onGarrisonChanged -= RefreshCommanders;
			PolisData.Current.onNextPromotableCommanderChanged -= RefreshPromotion;
			PolisData.Current.StockedItems.onChanged -= RefreshBattalionArea;
		}

		void Refresh()
		{
			RefreshPromotion();
			RefreshCommanders();
		}
		#endregion

		#region 提拔
		[SerializeField] Button promotionButton;
		[SerializeField] LayoutGroup promotionSlot;  // 暂时不用，如果将来改成同时可提拔多个再开。
		[SerializeField] FancyListItem promotionItem;

		void RefreshPromotion()
		{
			var promotion = PolisData.Current.GetNextPromotion();

			promotionItem.ApplyCommander(promotion.commander);
			promotionItem.SetCosts(promotion.cost);
			promotionItem.Interactable = false;

			promotionButton.interactable = promotion.commander != null && PolisData.Current.Economy.TryCost(promotion.cost);
		}

		public void Promote()
		{
			PolisData.Current.PromoteCommander();
		}
		#endregion

		#region 指挥官列表
		[SerializeField] LayoutGroup commandersLayoutGroup;

		void RefreshCommanders()
		{
			commandersLayoutGroup.transform.ClearChildren();
			var currentCommanders = PolisData.Current.GetGarrisonedCommanders();
			foreach(var commander in currentCommanders)
			{
				FancyListItem item = FancyListItem.Instantiate(commandersLayoutGroup.transform);
				item.ApplyCommander(commander);
				item.onClick += () => OnSelectCommander(commander);
			}
		}

		BattalionStatus selectedBattalion;
		BattalionStatus SelectedBattalion
		{
			get => selectedBattalion;
			set
			{
				if(selectedBattalion != null)
					selectedBattalion.onChanged -= RefreshBattalionArea;

				selectedBattalion = value;

				if(selectedBattalion != null)
					selectedBattalion.onChanged += RefreshBattalionArea;
			}
		}
		GameCommander SelectedCommander => SelectedBattalion?.battalionCommander;

		void OnSelectCommander(GameCommander c)
		{
			SelectedBattalion = PolisData.Current.GetGarrisonedBattalionByCommander(c);
			InspectCommander();
		}
		#endregion

		#region 指挥官检视
		[Header("指挥官细节")]
		[SerializeField] Image commanderAvatarImage;
		[SerializeField] TMP_Text commanderNameText;
		[SerializeField] TMP_Text zhiText, xinText, renText, yongText, yanText;
		[SerializeField] EquipmentSlot[] equipmentSlots;

		void InspectCommander()
		{
			commanderAvatarImage.gameObject.SetActive(SelectedCommander?.portrait != null);
			commanderAvatarImage.sprite = SelectedCommander?.portrait;
			commanderNameText.text = SelectedCommander?.commanderName ?? string.Empty;

			zhiText.text = SelectedCommander?.Zhi.ToString();
			xinText.text = SelectedCommander?.Xin.ToString();
			renText.text = SelectedCommander?.Ren.ToString();
			yongText.text = SelectedCommander?.Yong.ToString();
			yanText.text = SelectedCommander?.Yan.ToString();

			RefreshEquipmentSlots();
			SyncBattalionTypeDropDown();
			RefreshBattalionArea();
		}

		void SetupEquipmentSlots()
		{
			for(int i = 0; i < equipmentSlots.Length; ++i)
			{
				equipmentSlots[i].onSelected += e => PolisData.Current.EquipForCommander(SelectedCommander, e, i);
			}
		}

		static bool IsEquipment(ItemDefinition item)
		{
			return item != null && item is EquipmentDefinition;
		}

		void RefreshEquipmentSlots()
		{
			var items = PolisData.Current.Economy.items.ToArray();
			for(int i = 0; i < equipmentSlots.Length; ++i)
			{
				var slot = equipmentSlots[i];
				slot.Options = PolisData.Current.Economy.items
					.Where(record => IsEquipment(record.Definition))
					.Select(r => r.Definition as EquipmentDefinition);
				slot.DisplaySprite = SelectedCommander == null ? null : SelectedCommander.equipments[i]?.icon;
			}
		}

		[Header("军队编制")]
		[SerializeField] CanvasGroup battalionsArea;

		[SerializeField] TMP_Text weaponNameText;
		[SerializeField] TMP_Text weaponCountText;
		[SerializeField] TMP_Text populationText;

		[SerializeField] TMP_Dropdown battalionTypeDropDown;
		[SerializeField] Slider battalionSlider;
		[SerializeField] TMP_Text currentCountText, availableCountText;
		BattalionDefinition[] trainableBattalionDefinitions;

		void SetupBattalionArea()
		{
			trainableBattalionDefinitions = UnitDatabase.BattalionDefinitionSheet.unitDefinitions
				.OfType<BattalionDefinition>()
				.Where(d => d != null && d.isTrainable)
				.ToArray();

			battalionTypeDropDown.ClearOptions();
			battalionTypeDropDown.AddOptions(trainableBattalionDefinitions
				.Select(d => new TMP_Dropdown.OptionData()
				{
					text = d.name,
				}).ToList());
			battalionTypeDropDown.onValueChanged.AddListener(OnSelectBattalionType);
		}

		void OnSelectBattalionType(int i)
		{
			if(SelectedBattalion == null)
				return;
			if(i < 0 || i >= (trainableBattalionDefinitions?.Length ?? 0))
				return;

			var definition = trainableBattalionDefinitions[i];
			if(definition == null)
				return;

			// 切兵种的资源结算走数据层，避免只在 UI 层改显示。
			PolisData.Current.TryChangeBattalionDefinition(SelectedBattalion, definition);
		}

		void SyncBattalionTypeDropDown()
		{
			if(SelectedBattalion?.battalionDefinition == null)
				return;

			int index = System.Array.IndexOf(trainableBattalionDefinitions, SelectedBattalion.battalionDefinition);
			if(index >= 0)
				battalionTypeDropDown.SetValueWithoutNotify(index);
		}

		BattalionDefinition GetSelectedBattalionDefinition()
		{
			if(trainableBattalionDefinitions == null || trainableBattalionDefinitions.Length == 0)
				return SelectedBattalion?.battalionDefinition;

			int index = battalionTypeDropDown.value;
			if(index < 0 || index >= trainableBattalionDefinitions.Length)
				return SelectedBattalion?.battalionDefinition;

			return trainableBattalionDefinitions[index] ?? SelectedBattalion?.battalionDefinition;
		}

		int GetRequiredWeaponStock(BattalionDefinition definition)
		{
			if(definition?.requiredWeapon == null)
				return int.MaxValue;

			if(string.IsNullOrEmpty(definition.requiredWeapon.itemId))
				return 0;

			return PolisData.Current.StockedItems.GetItemQuantity(definition.requiredWeapon.itemId);
		}

		void RefreshBattalionArea()
		{
			battalionsArea.interactable = SelectedBattalion != null;
			battalionsArea.alpha = SelectedBattalion == null ? 0 : 1;
			if(SelectedBattalion == null)
				return;

			var definition = GetSelectedBattalionDefinition();
			if(definition?.requiredWeapon == null)
			{
				weaponNameText.text = "无";
				weaponCountText.text = "不限制";
			}
			else
			{
				weaponNameText.text = string.IsNullOrEmpty(definition.requiredWeapon.itemName) ? definition.requiredWeapon.name : definition.requiredWeapon.itemName;
				weaponCountText.text = GetRequiredWeaponStock(definition).ToString();
			}

			populationText.text = PolisData.Current.FreePopulation.ToString();
			currentCountText.text = SelectedBattalion.currentSolider.ToString();

			battalionSlider.onValueChanged.RemoveListener(OnSetSoldierCount);
			int cap = CalculateSoliderCap();
			battalionSlider.maxValue = Mathf.Max(cap, 0);
			battalionSlider.value = SelectedBattalion.currentSolider;
			battalionSlider.onValueChanged.AddListener(OnSetSoldierCount);

			availableCountText.text = cap.ToString();
		}

		int CalculateSoliderCap()
		{
			if(SelectedBattalion == null)
				return default;

			int populationCap = PolisData.Current.FreePopulation + SelectedBattalion.currentSolider;
			int weaponCap = int.MaxValue;

			var definition = GetSelectedBattalionDefinition();
			if(definition?.requiredWeapon != null)
			{
				// 当前兵员已视作占用完毕，库存仅限制“新增”可募兵数量。
				weaponCap = SelectedBattalion.currentSolider + GetRequiredWeaponStock(definition);
			}

			return Mathf.Min(populationCap, weaponCap);
		}

		void OnSetSoldierCount(float v)
		{
			int count = Mathf.RoundToInt(v);
			PolisData.Current.SetBattalionSoldierCount(SelectedBattalion, count);
		}
		#endregion
	}
}
