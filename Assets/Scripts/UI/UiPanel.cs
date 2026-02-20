using UnityEngine;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 在打开/关闭时暂停/恢复游戏。
	/// </summary>
	public class UiPanel : MonoBehaviour
	{
		[SerializeField] bool pauseOnOpen = true;
		public bool PauseOnOpen
		{
			get => pauseOnOpen;
			set => pauseOnOpen = value;
		}

		bool isPausedWhenOpened;

		protected void Start()
		{
			isPausedWhenOpened = GameInstance.Instance.Paused;
			GameInstance.Instance.Paused = PauseOnOpen;
		}

		protected void OnDestroy()
		{
			if(UiManager.Instance && UiManager.Instance.IsAnyModalOpen)
				return;

			GameInstance.Instance.Paused = isPausedWhenOpened;
		}
	}
}
