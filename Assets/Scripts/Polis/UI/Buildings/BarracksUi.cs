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
			GameCommander commander = PolisData.Current.GetPromotableCommander();
			promotionItem.ApplyCommander(commander);
			promotionItem.Interactable = false;
			promotionButton.interactable = commander != null;
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

			RefreshBattalionArea();
		}

		[Header("军队编制")]
		[SerializeField] CanvasGroup battalionsArea;

		[SerializeField] TMP_Text weaponNameText;
		[SerializeField] TMP_Text weaponCountText;
		[SerializeField] TMP_Text populationText;

		[SerializeField] TMP_Dropdown battalionTypeDropDown;
		[SerializeField] Slider battalionSlider;
		[SerializeField] TMP_Text currentCountText, availableCountText;

		void SetupBattalionArea()
		{
			battalionTypeDropDown.ClearOptions();
			battalionTypeDropDown.AddOptions(UnitDatabase.BattalionDefinitionSheet.unitDefinitions
				.Where(d => (d as BattalionDefinition).isTrainable)
				.Select(d => new TMP_Dropdown.OptionData()
				{
					text = d.name,
				}).ToList());
			battalionTypeDropDown.onValueChanged.AddListener(OnSelectBattalionType);
		}

		void OnSelectBattalionType(int i)
		{
			if(SelectedCommander == null)
				return;
		}

		void RefreshBattalionArea()
		{
			battalionsArea.interactable = SelectedBattalion != null;
			battalionsArea.alpha = SelectedBattalion == null ? 0 : 1;
			if(SelectedBattalion == null)
				return;

			weaponNameText.text = "";  // TODO
			weaponCountText.text = "";  // TODO

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

			int res = PolisData.Current.FreePopulation + SelectedBattalion.currentSolider;
			return res;
		}

		void OnSetSoldierCount(float v)
		{
			int count = Mathf.RoundToInt(v);
			PolisData.Current.SetBattalionSoldierCount(SelectedBattalion, count);
		}
		#endregion
	}
}
