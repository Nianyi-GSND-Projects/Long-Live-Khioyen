using UnityEngine;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		public void PlanFutureDialog(int dialogId, float delay = 0)
		{
			PolisTask task = new(PolisTaskType.startDialog, delay, 0, $"{dialogId}");
			AddTask(task);
		}

		void ExecuteStartDialogTask(PolisTask task)
		{
			var dialogId = int.Parse(task.parameters[0]);  // TODO: 应改为 string 更合理
			var dialog = DialogDatabase.Instance.GetDialog(dialogId);
			if(dialog == null)
			{
				Debug.LogWarning($"尝试开始 ID 为 {dialogId} 的对话，无法找到。");
				return;
			}

			Debug.Log($"开始 ID 为 {dialogId} 的对话。");
			var dialogUi = EventDialogUI.GetOrCreateInstance();
			var uiManager = UiManager.Instance;
			dialogUi.onHidden += uiManager.CloseCurrentUiModal;
			uiManager.OpenUiModal(dialogUi.gameObject, true);
			dialogUi.StartDialogChain(dialog);
		}
	}
}