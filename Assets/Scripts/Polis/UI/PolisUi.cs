using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public class PolisUi : MonoBehaviour
	{
		public Polis polis;

		#region Life cycle
		protected void Start()
		{
			localizedPolisName = polis.Data.GetLocalizedName();
			localizedPolisName.StringChanged += s => polisName.text = s;

			polis.onPopulationDataChanged += UpdatePopulation;
			UpdatePopulation();

			polis.onEconomyChanged += UpdateEnocomy;
			UpdateEnocomy();

			SwitchBottomPanel(normalPanel);

			polis.onSelectionChanged += OnSelectionChanged;
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
		[Header("Population")]
		public TMP_Text populationValue;

		void UpdatePopulation()
		{
			populationValue.text = $"{polis.Population}/{polis.FreePopulation}/{polis.PopulationCap}";
		}

		[Header("Economy")]
		public TMP_Text foodValue;
		public TMP_Text materialValue;
		public TMP_Text moneyValue;

		void UpdateEnocomy()
		{
			foodValue.text = $"{(int)polis.Economy.food}";
			materialValue.text = $"{(int)polis.Economy.material}";
			moneyValue.text = $"{(int)polis.Economy.money}";
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
		public Transform inspectionArea;
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

		void SetBottomAreaVisibiltiy(bool visible)
		{
			bottomArea.gameObject.SetActive(visible);
		}
		#endregion

		#region Side bar
		[Header("Side Bar")]
		public Image switchModeImage;
		public Sprite wanderModeIcon;
		public Sprite mayorModeIcon;

		public void SwitchMode()
		{
			polis.SwitchMode();
			switch(polis.CurrentMode)
			{
				case Polis.Mode.Mayor:
					switchModeImage.sprite = wanderModeIcon;
					SetBottomAreaVisibiltiy(true);
					break;
				case Polis.Mode.Wander:
					ExitConstructModal();
					switchModeImage.sprite = mayorModeIcon;
					SetBottomAreaVisibiltiy(false);
					break;
			}
		}
		#endregion

		#region Construction
		public void EnterConstructModal()
		{
			SwitchBottomPanel(constructPanel);
			polis.IsInConstructModal = true;
		}

		public void ExitConstructModal()
		{
			SwitchBottomPanel(normalPanel);
			polis.IsInConstructModal = false;
		}
		#endregion

		#region Inspection
		GameObject inspectionUi;

		void OnSelectionChanged(ISelectable selected)
		{
			if(inspectionUi != null)
			{
				Destroy(inspectionUi);
				inspectionUi = null;
			}

			if(selected is Component && (selected as Component).TryGetComponent(out IInspectable inspectable))
			{
				inspectionUi = inspectable.MakeUi();
				if(inspectionUi != null)
					inspectionUi.transform.SetParent(inspectionArea, false);
			}
		}
		#endregion
	}
}
