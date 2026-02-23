using UnityEngine;
using TMPro;
using System.Linq;

namespace LongLiveKhioyen
{
	public class MonthPassUi : MonoBehaviour
	{
		[SerializeField] TMP_Text debugContent;

		protected void Start()
		{
			debugContent.text = string.Join(",", PolisData.Current.MonthlyResourceChanges.Select(r => r.ToString()));
		}
	}
}
