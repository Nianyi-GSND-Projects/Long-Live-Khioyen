using UnityEngine;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 此类只有 Assets/Unity Event Agent 一个 ScriptableObject 实例，用于方便在 Unity inspector 里调用方法。
	/// </summary>
	[CreateAssetMenu(menuName = "Long Live Khioyen/Unity Event Agent")]
	public class UnityEventAgent : ScriptableObject
	{
		#region Game manager
		public void StartNewGame()
		{
			GameManager.StartNewGame();
		}

		public void StopCurrentGame()
		{
			GameManager.StopCurrentGame();
		}

		public void ExitGame()
		{
			GameManager.ExitGame();
		}
		#endregion

		#region Game Instance
		public void OpenPauseMenu()
		{
			GameInstance.Instance.OpenPauseMenu();
		}
		#endregion

		#region UI
		public void CloseCurrentUi()
		{
			UiManager.Instance.CloseCurrentUi();
		}

		public void OpenUiFromTemplate(GameObject template)
		{
			UiManager.Instance.OpenUiFromTemplate(template);
		}
		#endregion
	}
}
