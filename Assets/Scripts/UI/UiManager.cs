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

		#region Events
		public static System.Action onUiStateChanged;
		#endregion

		#region Modal
		struct UiModalRecord
		{
			public GameObject go;
			public bool isInstantiated;
		}

		readonly Stack<UiModalRecord> modalStack = new();

		public bool IsAnyModalOpen => modalStack.Count > 0;

		public void OpenUiModal(GameObject go, bool isInstantiated)
		{
			if(go == null)
			{
				Debug.LogWarning("The UI to be opened is null.");
				return;
			}
			if(Instance == null)
			{
				Debug.LogWarning("UI can only be opened when a game instance is running.");
				return;
			}

			go.transform.SetParent(transform, false);
			modalStack.Push(new() {
				go = go,
				isInstantiated = isInstantiated,
			});

			onUiStateChanged?.Invoke();
		}

		public void OpenUiModalFromTemplate(GameObject template)
		{
			OpenUiModal(Instantiate(template), true);
		}

		public void OpenUiModalFromPrefabPath(string prefabPath)
		{
			OpenUiModalFromTemplate(Resources.Load<GameObject>(prefabPath));
		}

		public void CloseCurrentUiModal()
		{
			if(modalStack.Count == 0)
			{
				Debug.LogWarning("No UI is currently open.");
				return;
			}

			var modal = modalStack.Pop();
			if(modal.isInstantiated)
			{
				Destroy(modal.go);
			}
			else
			{
				if(modal.go != null)
					modal.go.SetActive(false);
			}

				onUiStateChanged?.Invoke();
		}
		#endregion
	}
}
