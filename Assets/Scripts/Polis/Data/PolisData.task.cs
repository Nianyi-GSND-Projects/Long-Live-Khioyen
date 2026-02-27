using UnityEngine;
using System;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
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
			NotifyPossiblePopulationChange();
		}

		/// <remarks>此方法内及下游不需删除 task；删除的逻辑在 PassTime_Simple 中。</remarks>
		void ExecuteTask(PolisTask task)
		{
			switch(task.type)
			{
				case PolisTaskType.buildingConstructed:
					ExecuteBuildingConstructedTask(task);
					break;
				case PolisTaskType.itemProduced:
					ExecuteCompleteProductionTask(task);
					break;
				case PolisTaskType.startDialog:
					ExecuteStartDialogTask(task);
					break;
				case PolisTaskType.scheduleDialog:
					ExecuteScheduleDialogTask(task);
					break;
				default: throw new NotSupportedException();
			}
		}
	}

	[Serializable]
	public class PolisTask
	{
		public string type;
		public float totalTime;
		public float remainingTime;
		public int requiredPopulation;
		public string[] parameters;

		public PolisTask(string type, float totalTime, int requiredPopulation, params string[] parameters)
		{
			this.type = type;
			this.totalTime = totalTime;
			remainingTime = totalTime;
			this.requiredPopulation = requiredPopulation;
			this.parameters = parameters;
		}

		public PolisTask(string type, float totalTime, params string[] parameters)
			: this(type, totalTime, 0, parameters) { }
	}

	public static class PolisTaskType
	{
		public const string buildingConstructed = "building-constructed";
		public const string itemProduced = "item-produced";
		public const string startDialog = "start-dialog";
		public const string scheduleDialog = "schedule-dialog";
	}
}
