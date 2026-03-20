using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using Nianyi.UnityPack;

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

			Refresh();
			InspectCommander();
		}

		protected void OnDestroy()
		{
			SelectedBattalion = null;  // 解绑事件
			PolisData.Current.onGarrisonChanged -= RefreshCommanders;
			PolisData.Current.onNextPromotableCommanderChanged -= RefreshPromotion;
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
				slot.DisplayedEquipment = SelectedCommander?.equipments[i];
			}
		}

		[Header("军队编制")]
		[SerializeField] CanvasGroup battalionsArea;

		[SerializeField] LayoutGroup statisticsArea;

		[SerializeField] TMP_Dropdown battalionTypeDropDown;
		[SerializeField] Slider battalionSlider;
		[SerializeField] TMP_Text currentCountText, availableCountText;
		BattalionDefinition[] trainableBattalionDefinitions;
		[SerializeField] TMP_Text battalionDescriptionText;

		void OnSelectBattalionType(int i)
		{
			PolisData.Current.SetBattalionType(SelectedBattalion, trainableBattalionDefinitions[i]);
		}

		void OnSetSoldierCount(float v)
		{
			int count = Mathf.RoundToInt(v);
			PolisData.Current.SetBattalionCount(SelectedBattalion, count);
		}

		void SetupBattalionArea()
		{
			trainableBattalionDefinitions = PolisData.Current.GetTrainableBattalions().ToArray();

			battalionTypeDropDown.ClearOptions();
			battalionTypeDropDown.AddOptions(trainableBattalionDefinitions
				.Select(d => new TMP_Dropdown.OptionData()
				{
					text = d.name,
				}).ToList());
			battalionTypeDropDown.onValueChanged.AddListener(OnSelectBattalionType);

			battalionSlider.onValueChanged.AddListener(OnSetSoldierCount);

			RefreshBattalionArea();
		}

		void RefreshBattalionArea()
		{
			battalionsArea.interactable = SelectedBattalion != null;
			battalionsArea.alpha = SelectedBattalion == null ? 0 : 1;
			if(SelectedBattalion == null)
				return;

			statisticsArea.transform.ClearChildren();
			foreach(var s in GetStatistics())
			{
				var pf = HierarchyUtility.InstantiatePrefabFromResource("Prefabs/UI/Common/Property Field - dark");
				pf.transform.SetParent(statisticsArea.transform, false);
				var texts = pf.transform.GetComponentsInChildren<TMP_Text>();
				texts[0].text = s.Key;
				texts[1].text = $"{s.Value}";
			}

			battalionSlider.SetValueWithoutNotify(SelectedBattalion.currentSolider);
			int cap = PolisData.Current.GetBattalionCap(SelectedBattalion, SelectedBattalion.battalionDefinition);
			battalionSlider.maxValue = Mathf.Max(cap, 0);

			currentCountText.text = SelectedBattalion.currentSolider.ToString();
			availableCountText.text = cap.ToString();

			battalionDescriptionText.text = SelectedBattalion.battalionDefinition.LocalizedDescription;
		}

		IEnumerable<KeyValuePair<string, int>> GetStatistics()
		{
			var pd = PolisData.Current;
			foreach(var item in SelectedBattalion.battalionDefinition.requiredItems)
			{
				int count = Mathf.FloorToInt(pd.Economy.Get(new() { type = ResourceType.Item, itemId = item.itemId }));
				yield return new(item.itemName, count);
			}
			yield return new("Free population", pd.FreePopulation);
		}
		#endregion
	}
}
