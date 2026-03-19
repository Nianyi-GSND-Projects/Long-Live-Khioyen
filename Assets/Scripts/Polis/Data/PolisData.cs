using UnityEngine;
using UnityEngine.Localization;
using System;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
	[Serializable]
	public partial class PolisData
	{
		public static PolisData Current => Polis.Instance.Data;
		public static PolisData Main => GameInstance.Instance.Data.GetPolis(GameInstance.Instance.Data.mainPolis);

		public string id;
		public string LocalizedName => new LocalizedString("Polis Names", id).GetLocalizedString();
		public bool canControl = false;
		public bool conquered = false;
		public PolisType type;

		public Vector2 position;
		public Vector2Int size;
		[Range(0, 359)] public float orientation;

		[Label("图标重载")] public Sprite iconOverride;
		[Label("被攻克后的图标重载"), HideIf(nameof(type), PolisType.Friendly)] public Sprite conqueredIconOverride;
		public Sprite Sprite
		{
			get
			{
				var s = GameManager.InternalSettings;
				var overrideIcon = !conquered ? iconOverride : conqueredIconOverride;
				if(overrideIcon != null)
					return overrideIcon;
				return type switch
				{
					PolisType.Hostile => s.fallbackHostileIcon,
					PolisType.Friendly => s.fallbackFriendlyIcon,
					PolisType.Controlled => s.fallbackFriendlyIcon,
					_ => null,
				};
			}
		}

		public PolisData()
		{
			lastTime.onMonthPassed += OnMonthPassed;
		}
	}

	public enum PolisType
	{
		Undefined,
		Controlled, Hostile, Friendly,
	}
}