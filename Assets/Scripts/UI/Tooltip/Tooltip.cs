using UnityEngine;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
	public class Tooltip : MonoBehaviour, ITooltipSource
	{
		[SerializeField] string tooltipText;
		public string TooltipText
		{
			get => tooltipText;
			set => tooltipText = value;
		}

		public string GetTooltipText()
		{
			return tooltipText;
		}

		[SerializeField] bool overrideDelay = false;
		[SerializeField, Min(0), ShowIf(nameof(overrideDelay))] float delay = 1f;
		public float Delay
		{
			get
			{
				if(!overrideDelay)
					return GameManager.InternalSettings.tooltipDelay;
				return delay;
			}
			set
			{
				overrideDelay = true;
				delay = value;
			}
		}
	}
}
