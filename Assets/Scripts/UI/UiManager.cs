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
		public static System.Action<GameObject> onUiOpened;
		public static System.Action<GameObject> onUiClosed;
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
				Debug.LogError("The UI to be opened is null.");
				return;
			}

			go.transform.SetParent(transform, false);
			modalStack.Push(new() {
				go = go,
				isInstantiated = isInstantiated,
			});

			onUiOpened?.Invoke(go);
			onUiStateChanged?.Invoke();
		}

		public void OpenUiModalFromTemplate(GameObject template)
		{
			if(template == null)
			{
				Debug.LogError($"The UI template to be opened is null.");
				return;
			}
			OpenUiModal(Instantiate(template), true);
		}

		public void OpenUiModalFromPrefabPath(string prefabPath)
		{
			var prefab = Resources.Load<GameObject>(prefabPath);
			if(prefab == null)
			{
				Debug.LogError($"The UI prefab at {prefabPath} doesn't exist.");
				return;
			}
			OpenUiModalFromTemplate(prefab);
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

			onUiClosed?.Invoke(modal.go);
			onUiStateChanged?.Invoke();
		}
		#endregion
	}
}
