using UnityEngine;
using System;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		public void ScheduleDialog(int dialogId, float delay = 0, bool instant = false)
		{
			var taskType = instant ? PolisTaskType.startDialog : PolisTaskType.scheduleDialog;
			PolisTask task = new(taskType, delay, 0, $"{dialogId}");
			AddTask(task);
		}

		public Action<int> onStartDialog;

		void ExecuteStartDialogTask(PolisTask task)
		{
			var dialogId = int.Parse(task.parameters[0]);
			onStartDialog?.Invoke(dialogId);
		}

		void ExecuteScheduleDialogTask(PolisTask task)
		{
			var dialogId = int.Parse(task.parameters[0]);
			float delayTime = float.Parse(task.parameters[1]);

			ScheduleDialog(dialogId, delayTime, true);
		}
	}
}