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
		/// <summary>按月计。</summary>
		public void PassTime(float dMonth)
		{
			if(Efficiency == 0)
			{
				// 效率为 0（人口也为 0）时无法执行任何任务，可安全度过时间。
				PassTime_Simple(dMonth);
				return;
			}

			while(dMonth > 0)
			{
				if(Tasks.Count == 0)
				{
					PassTime_Simple(dMonth);
					return;
				}
				float a = Mathf.Min(dMonth, Tasks[0].remainingTime / Efficiency);
				PassTime_Simple(a);
				dMonth -= a;
			}
		}

		void PassTime_Simple(float dMonth)
		{
			foreach(var task in Tasks)
				task.remainingTime -= dMonth * Efficiency;
			var toBeExecuted = Tasks.Where(t => t.remainingTime <= 0).ToArray();
			foreach(var task in toBeExecuted)
			{
				RemoveTask(task);
				ExecuteTask(task);
			}
			lastTime.AdvanceByMonth(dMonth);
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
