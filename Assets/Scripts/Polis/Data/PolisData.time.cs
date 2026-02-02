using UnityEngine;
using System;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		/// <summary>最后一次更新过此城池状态的游戏时间。</summary>
		[SerializeField] float lastTime;

		public float LastTime
		{
			get => lastTime;
			set => lastTime = value;
		}

		#region 事件
		public Action onMonthPassed;
		#endregion

		#region 接口
		public void PassTime(float amount)
		{
			int previousMonth = GameManager.ConvertToMonth(GameInstance.Instance.GameTime - amount);
			int currentMonth = GameInstance.Instance.CurrentMonth;
			bool willPassMonth = currentMonth != 0 && previousMonth != currentMonth;

			PassTime_Internal(amount);

			if(willPassMonth)
			{
				// 这些信息现在还没用到，之后会显示在月度更迭的界面里。
#pragma warning disable CS0219
				int monthsPassed = currentMonth - previousMonth;
				bool justReturned = false;  // 是否刚从外面回城。
#pragma warning restore CS0219

				onMonthPassed?.Invoke();
			}
		}
		#endregion

		#region 任务
		void ExecuteMonthPassedTask(PolisTask task)
		{
			int startingMonth = int.Parse(task.parameters[0]);
			Debug.Log($"Month passed in polis \"{id}\". Starting month: {startingMonth}");
			// TODO
		}
		#endregion

		#region 辅助
		void PassTime_Internal(float amount)
		{
			if(Efficiency == 0)
			{
				// 效率为 0（人口也为 0）时无法执行任何任务，可安全度过时间。
				PassTime_Simple(amount);
				return;
			}

			while(amount > 0)
			{
				if(Tasks.Count == 0)
				{
					PassTime_Simple(amount);
					return;
				}
				float a = Mathf.Min(amount, Tasks[0].remainingTime / Efficiency);
				PassTime_Simple(a);
				amount -= a;
			}
		}

		void PassTime_Simple(float amount)
		{
			foreach(var task in Tasks)
				task.remainingTime -= amount * Efficiency;
			var toBeExecuted = Tasks.Where(t => t.remainingTime <= 0).ToArray();
			foreach(var task in toBeExecuted)
			{
				ExecuteTask(task);
				RemoveTask(task);
			}
			LastTime += amount;
		}
		#endregion
	}
}
