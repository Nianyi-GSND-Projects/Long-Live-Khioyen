using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LongLiveKhioyen
{
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

	/// <summary>
	/// 经济资源种类描述符，主要用于方便地指代几种预定义的资源。
	/// </summary>
	public enum EconomyType
	{
		Undefined = 0b0,
		Food = 0b100, Material, Money,
		Custom = 0xffff,
	}
}
