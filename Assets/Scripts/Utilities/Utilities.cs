using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public static class Utilities
	{
		public static T DeepCopy<T>(T source)
		{
			return JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
		}

		public static Vector3 GetRandomPositionOnNavMesh(this NavMeshSurface surface)
		{
			var bounds = surface.navMeshData.sourceBounds;
			for(int i = 0; i < 30; i++)
			{
				Vector3 random = new(
					Random.Range(bounds.min.x, bounds.max.x),
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z)
				);
				Vector3 world = surface.transform.TransformPoint(random);

				if(NavMesh.SamplePosition(world, out NavMeshHit hit, bounds.size.y, NavMesh.AllAreas))
					return hit.position;
			}
			return default;
		}

		public static void ClearChildren(this Transform parent)
		{
			if(parent == null)
				return;
			foreach(var c in parent.GetDirectChildren())
				c.gameObject.Destroy();
		}

		#region 时间
		/// <summary>将自开始以来的游戏逻辑时间转换成历史上的绝对时间的本地化表述。</summary>
		public static string LocalizeTimeSinceGameStart(float gameTime, string locale = "en")
		{
			int month = (int)MathUtility.Mod(gameTime, 12);
			int year = (int)((gameTime + startTime) / 12);
			return $"{LocalizeMonth(month)}, {LocalizeYear(year)}";
		}

		static string[] englishMonths = new string[]
		{ "January", "Febuary", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

		/// <summary>本地化月份。</summary>
		/// <remarks>0 对应一月，11 对应十二月，会自动取余。</remarks>
		public static string LocalizeMonth(int month, string locale = "en")
		{
			int i = (int)MathUtility.Mod(month, 12);
			return englishMonths[i];
		}

		/// <summary>游戏开始于公元前 121 年。</summary>
		const int startTime = -121 * 12;

		/// <summary>本地化（绝对）年份。</summary>
		/// <remarks>
		/// 公元前的年份是 as-is 的，即 -1 对应公元前 1 年；
		/// 公元后的年份要减一，即 0 对应公元元年。
		/// </remarks>
		public static string LocalizeYear(int year, string locale = "en")
		{
			bool isAd = year >= 0;
			return isAd ? $"{year + 1} AD" : $"{-year} BC";
		}
		#endregion
	}
}
