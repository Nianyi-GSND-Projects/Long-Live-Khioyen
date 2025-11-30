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

		public bool isControlled;
		public bool isHostile;

		public Vector2Int size;
		public Economy economy;
		/// <summary>最后一次更新过此城池状态的游戏时间。</summary>
		public float lastTime;
		public List<BuildingPlacement> buildings;

		[SerializeField] List<PolisTask> tasks;
		public IList<PolisTask> Tasks => tasks;
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
	}

	[Serializable]
	public struct Economy
	{
		public float food;
		public float material;
		public float money;

		public override readonly string ToString()
		{
			return $"(food:{food}, money:{money}, knowledge:{material})";
		}

		static IEnumerable<(float, float)> ValuePairs(in Economy a, in Economy b)
		{
			return new (float, float)[] {
				(a.food, b.food),
				(a.material, b.material),
				(a.money, b.money),
			};
		}
		static bool Compare(in Economy a, in Economy b, Func<float, float, bool> comparer)
		{
			return ValuePairs(a, b)
				.Select(pair => comparer.Invoke(pair.Item1, pair.Item2))
				.Aggregate((a, b) => a && b);
		}

		public static bool operator <(in Economy a, in Economy b) => Compare(a, b, (a, b) => a < b);
		public static bool operator >(in Economy a, in Economy b) => Compare(a, b, (a, b) => a > b);
		public static bool operator <=(in Economy a, in Economy b) => Compare(a, b, (a, b) => a <= b);
		public static bool operator >=(in Economy a, in Economy b) => Compare(a, b, (a, b) => a >= b);

		public static Economy operator -(in Economy a, in Economy b)
		{
			Economy result = a;
			result.food -= b.food;
			result.money -= b.money;
			result.material -= b.material;
			return result;
		}
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
	}

	public static class PolisTaskType
	{
		public const string construction = "construction";
		public const string monthPassed = "month-passed";
	}
}