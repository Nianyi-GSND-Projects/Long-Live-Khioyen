using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class PauseMenu : MonoBehaviour
	{
		public Button saveButton;

		protected void OnEnable()
		{
			var isInPolis = GameInstance.Instance.CurrentMode == GameInstance.Mode.Polis;
			saveButton.interactable = isInPolis;
		}

		public void Close()
		{
			UiManager.Instance.CloseCurrentUi();
		}

		public void QuitGame()
		{
			GameManager.StopCurrentGame();
		}
	}
}
