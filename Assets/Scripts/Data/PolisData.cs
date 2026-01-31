using UnityEngine;
using UnityEngine.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	[Serializable]
	public class PolisData
	{
		public string id;
		public Vector2 position;
		[Range(0, 359)] public float orientation;

		public LocalizedString GetLocalizedName()
		{
			return new("Polis Names", id);
		}

		public PolisType type;

		public Vector2Int size;
		public int population;
		public Economy economy;
		public List<BuildingPlacement> buildings;

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

	public enum PolisType
	{
		Undefined,
		Controlled, Hostile,
	}

	[Serializable]
	public class BuildingPlacement
	{
		public string id;  // The building ID stored in the definition sheet.
		public Vector2Int position;
		[Range(0, 3)] public int orientation;  // By 90 degrees.

		public bool underConstruction;
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
	}
}