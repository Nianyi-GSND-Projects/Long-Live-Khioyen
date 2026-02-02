using UnityEngine;
using System;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		/// <summary>最后一次更新过此城池状态的游戏时间。</summary>
		public float lastTime;

		[SerializeField] List<PolisTask> tasks;
		public IReadOnlyList<PolisTask> Tasks => tasks;

		public void AddTask(PolisTask task)
		{
			tasks.Add(task);
			tasks.Sort((a, b) =>
			{
				float fa = a.remainingTime, fb = b.remainingTime;
				if(fa == fb)
					return 0;
				if(fa < fb)
					return -1;
				return 1;
			});
		}

		public void RemoveTask(PolisTask task)
		{
			tasks.Remove(task);
		}
	}

	[Serializable]
	public class PolisTask
	{
		public string type;
		public float remainingTime;
		public string[] parameters;
		public int requiredPopulation;
	}

	public static class PolisTaskType
	{
		public const string construction = "construction";
		public const string monthPassed = "month-passed";
		public const string completeProduction = "complete-production";
	}
}
