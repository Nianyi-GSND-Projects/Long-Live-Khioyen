using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public class PolisUi : MonoBehaviour
	{
		Polis Polis => Polis.Instance;

		#region Life cycle
		protected void Start()
		{
			localizedPolisName = Polis.Data.GetLocalizedName();
			localizedPolisName.StringChanged += s => polisName.text = s;

			Polis.onPopulationDataChanged += UpdatePopulation;
			UpdatePopulation();

			Polis.onEconomyChanged += UpdateEnocomy;
			UpdateEnocomy();

			SwitchBottomPanel(normalPanel);
			inspectionUi.Building = null;

			Polis.onSelectionChanged += OnSelectionChanged;
		}

		protected void Update()
		{
			UpdateTime();
		}
		#endregion

		#region General
		[Header("Status")]
		public CanvasGroup statusBar;
		public TMP_Text polisName;
		LocalizedString localizedPolisName;

		public void OpenPauseMenu()
		{
			GameInstance.Instance.OpenPauseMenu();
		}

		public void DepartFromPolis()
		{
			GameInstance.Instance.DepartFromPolis();
		}
		#endregion

		#region Status Bar
		[Header("Economy")]
		public TMP_Text foodValue;
		public TMP_Text materialValue;
		public TMP_Text moneyValue;

		void UpdateEnocomy()
		{
			foodValue.text = $"{(int)Polis.Economy.food}";
			materialValue.text = $"{(int)Polis.Economy.material}";
			moneyValue.text = $"{(int)Polis.Economy.money}";
		}

		[Header("Population")]
		public TMP_Text populationValue;
		public TMP_Text freePopulationValue;
		public TMP_Text populationCapValue;

		void UpdatePopulation()
		{
			populationValue.text = $"{Polis.Population}";
			freePopulationValue.text = $"{Polis.FreePopulation}";
			populationCapValue.text = $"{Polis.PopulationCap}";
		}

		[Header("Time")]
		public TMP_Text timeText;
		public Slider timeSlider;

		void UpdateTime()
		{
			float month = GameInstance.Instance.GameTime / GameManager.InternalSettings.monthLength;
			timeText.text = $"Month {Mathf.FloorToInt(month)}";
			timeSlider.value = month - Mathf.Floor(month);
		}
		#endregion

		#region Bottom Area
		[Header("Bottom Area")]
		public CanvasGroup bottomArea;
		public CanvasGroup normalPanel;
		public CanvasGroup constructPanel;

		void SwitchBottomPanel(CanvasGroup panel)
		{
			bool flag = false;  // Record if any panels has been switched to.

			for(int i = 0, count = bottomArea.transform.childCount; i < count; ++i)
			{
				var child = bottomArea.transform.GetChild(i);
				bool active = child == panel.transform;
				child.gameObject.SetActive(active);
				flag |= active;
			}

			if(!flag)  // If nothing has been switched to, display the normal panel.
				SwitchBottomPanel(normalPanel);
		}
		#endregion

		#region Side bar
		[Header("Side Bar")]
		public Image switchModeImage;
		public Sprite wanderModeIcon;
		public Sprite mayorModeIcon;

		public void SwitchMode()
		{
			Polis.SwitchMode();
			switch(Polis.CurrentMode)
			{
				case Polis.Mode.Mayor:
					switchModeImage.sprite = wanderModeIcon;
					break;
				case Polis.Mode.Wander:
					ExitConstructModal();
					switchModeImage.sprite = mayorModeIcon;
					break;
			}
		}
		#endregion

		#region Construction
		public void EnterConstructModal()
		{
			if(Polis.CurrentMode != Polis.Mode.Mayor)
				SwitchMode();
			SwitchBottomPanel(constructPanel);
			Polis.IsInConstructModal = true;
		}

		public void ExitConstructModal()
		{
			SwitchBottomPanel(normalPanel);
			Polis.IsInConstructModal = false;
		}
		#endregion

		#region Inspection
		[SerializeField] InspectionUi inspectionUi;

		void OnSelectionChanged(ISelectable selected)
		{
			inspectionUi.Building = selected is IBuildingLike ? (selected as IBuildingLike) : null;
		}
		#endregion

		#region Month pass
		[Header("Month pass")]
		public CanvasGroup monthPassPanel;

		public void ShowMonthPassPanel()
		{
			GameInstance.Instance.Paused = true;
			monthPassPanel.gameObject.SetActive(true);
		}

		public void HideMonthPassPanel()
		{
			monthPassPanel.gameObject.SetActive(false);
			GameInstance.Instance.Paused = false;
		}
		#endregion
	}
}
