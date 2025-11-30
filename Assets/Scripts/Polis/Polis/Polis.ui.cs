using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		#region Life cycle
		void InitializeUi()
		{
			localizedPolisName = new("Polis Names", "");
			localizedPolisName.StringChanged += s => polisName.text = s;

			onPopulationChanged += UpdateTopBar;
			onEconomyChanged += UpdateTopBar;
			UpdateTopBar();

			SwitchBottomPanel(normalPanel);
		}
		#endregion

		#region General
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
		[Header("Status Bar")]
		public CanvasGroup statusBar;
		public TMP_Text polisName;
		LocalizedString localizedPolisName;
		public TMP_Text populationValue;
		public TMP_Text foodValue;
		public TMP_Text materialValue;
		public TMP_Text moneyValue;

		void UpdateTopBar()
		{
			localizedPolisName.TableEntryReference = Id;
			localizedPolisName.RefreshString();
			populationValue.text = $"{Population}/{Population - BusyPopulation}/{PopulationCap}";
			foodValue.text = $"{(int)Economy.food}";
			materialValue.text = $"{(int)Economy.material}";
			moneyValue.text = $"{(int)Economy.money}";
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

		public void UiSwitchMode()
		{
			SwitchMode();
			switch(CurrentMode)
			{
				case Mode.Mayor:
					switchModeImage.sprite = wanderModeIcon;
					SetBottomAreaVisibiltiy(true);
					break;
				case Mode.Wander:
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
			IsInConstructModal = true;
		}

		public void ExitConstructModal()
		{
			SwitchBottomPanel(normalPanel);
			IsInConstructModal = false;
		}
		#endregion
	}
}
