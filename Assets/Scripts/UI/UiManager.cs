using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 管理互斥的 UI 面板的启用。
	/// </summary>
	public class UiManager : MonoBehaviour
	{
		#region Singleton
		static UiManager instance;
		public static UiManager Instance => instance;

		protected void Awake()
		{
			if(instance != null && instance != this)
			{
				Destroy(this);
				return;
			}
			instance = this;
		}

		protected void OnDestroy()
		{
			if(instance == this)
				instance = null;
		}
		#endregion

		#region UI
		readonly Stack<GameObject> uiStack = new();
		public bool IsAnyUiOpen => uiStack.Count > 0;
		public static System.Action onUiStateChanged;

		public void OpenUi(GameObject ui)
		{
			if(ui == null)
			{
				Debug.LogWarning("The UI to be opened is null.");
				return;
			}
			if(Instance == null)
			{
				Debug.LogWarning("UI can only be opened when a game instance is running.");
				return;
			}

			ui.transform.SetParent(transform, false);
			uiStack.Push(ui);

			onUiStateChanged?.Invoke();
		}

		public void OpenUiFromTemplate(GameObject template)
		{
			OpenUi(Instantiate(template));
		}

		public void OpenUiFromPrefabPath(string prefabPath)
		{
			OpenUiFromTemplate(Resources.Load<GameObject>(prefabPath));
		}

		public void CloseCurrentUi()
		{
			if(uiStack.Count == 0)
			{
				Debug.LogWarning("No UI is currently open.");
				return;
			}

			var ui = uiStack.Pop();
			Destroy(ui);

			onUiStateChanged?.Invoke();
		}
		#endregion
	}
}
