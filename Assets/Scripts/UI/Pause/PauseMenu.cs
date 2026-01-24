using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	[RequireComponent(typeof(ManyPanelUi))]
	public class PauseMenu : MonoBehaviour
	{
		ManyPanelUi panels;
		public CanvasGroup mainPanel, savePanel, settingsPanel;
		public Button saveButton;

		protected void Awake()
		{
			panels = GetComponent<ManyPanelUi>();
		}

		protected void OnEnable()
		{
			panels.SwitchToPanel(mainPanel);

			var isInPolis = GameInstance.Instance.CurrentMode == GameInstance.Mode.Polis;
			saveButton.interactable = isInPolis;
		}

		public void Close()
		{
			GameInstance.Instance.CloseCurrentUi();
		}

		public void QuitGame()
		{
			GameManager.StopCurrentGame();
		}
	}
}
