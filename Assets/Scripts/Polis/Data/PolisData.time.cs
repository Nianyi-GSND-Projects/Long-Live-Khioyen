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
			int previousMonth = Mathf.FloorToInt((GameInstance.Instance.Data.time.ElapsedGameTime - amount) / GameTime.MonthToGameTime);
			int currentMonth = GameInstance.Instance.Data.time.Month;
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
		/// <summary>
		/// 月度更迭时要执行的逻辑，可根据需求填充。
		/// </summary>
		void ExecuteMonthPassedTask(PolisTask task)
		{
			int startingMonth = int.Parse(task.parameters[0]);
			Debug.Log($"城池 {id} 度月。现开始 {GameTime.LocalizeMonth(startingMonth, "zh-Hans")}。");

			CashForSaleItemsAtEndOfMonth();
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
				RemoveTask(task);
				ExecuteTask(task);
			}
			LastTime += amount;
		}
		#endregion
	}
}
