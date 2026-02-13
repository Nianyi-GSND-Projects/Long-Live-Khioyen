using UnityEngine;
using UnityEngine.Localization;
using System;

namespace LongLiveKhioyen
{
	[Serializable]
	public partial class PolisData
	{
		public static PolisData Current => Polis.Instance.Data;

		public string id;
		public PolisType type;

		public Vector2 position;
		public Vector2Int size;
		[Range(0, 359)] public float orientation;

		public LocalizedString GetLocalizedName()
		{
			return new("Polis Names", id);
		}
	}

	public enum PolisType
	{
		Undefined,
		Controlled, Hostile,
	}
}