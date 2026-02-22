using UnityEngine;
using System;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		/// <summary>最后一次更新过此城池状态的游戏时间。</summary>
		[SerializeField] public GameTime lastTime = new();

		#region 度时
		public void PassTime(float amount)
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
			lastTime.AdvanceByInGameTime(amount);
		}
		#endregion

		#region 度月
		void OnMonthPassed()
		{
			UpdateResourcesMonthly();

			Debug.Log($"城池 {id} 度月。现开始 {GameTime.LocalizeMonth(lastTime.Month, "zh-Hans")}。");
		}
		#endregion
	}
}
