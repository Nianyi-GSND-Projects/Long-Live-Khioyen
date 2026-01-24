using UnityEngine;

namespace LongLiveKhioyen
{
	public class StartMenu : MonoBehaviour
	{
		public CanvasGroup mainPanel;

		public void StartNewGame()
		{
			GameManager.StartNewGame();
		}

		public void ExitGame()
		{
			GameManager.ExitGame();
		}
	}
}
