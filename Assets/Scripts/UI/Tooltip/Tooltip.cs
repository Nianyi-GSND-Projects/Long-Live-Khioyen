using UnityEngine;

namespace LongLiveKhioyen
{
	public class Tooltip : MonoBehaviour, ITooltipSource
	{
		[SerializeField] string tooltipText;

		public string GetTooltipText()
		{
			return tooltipText;
		}
	}
}
