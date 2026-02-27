using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		void InitializeDialog()
		{
			Data.onStartDialog += StartDialog;
		}

		void FinalizeDialog()
		{
			Data.onStartDialog -= StartDialog;
		}

		void StartDialog(int dialogId)
		{
			var dialog = DialogDatabase.Instance.GetDialog(dialogId);
			if(dialog == null)
			{
				Debug.LogWarning($"尝试开始 ID 为 {dialogId} 的对话，无法找到。");
				return;
			}

			Debug.Log($"开始 ID 为 {dialogId} 的对话。");
			var dialogUi = Instantiate(Resources.Load<GameObject>("Prefabs/UI/Events/Dialog Panel-Polis")).GetComponent<EventDialogUI>();
			var uiManager = UiManager.Instance;
			dialogUi.onHidden += uiManager.CloseCurrentUiModal;
			uiManager.OpenUiModal(dialogUi.gameObject, true);
			dialogUi.StartDialogChain(dialog);
		}
	}
}
