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
		public void CloseCurrentUiModal()
		{
			UiManager.Instance.CloseCurrentUiModal();
		}

		public void OpenUiModal(GameObject go)
		{
			UiManager.Instance.OpenUiModal(go, false);
		}

		public void OpenUiModalFromTemplate(GameObject template)
		{
			UiManager.Instance.OpenUiModalFromTemplate(template);
		}
		#endregion
	}
}
