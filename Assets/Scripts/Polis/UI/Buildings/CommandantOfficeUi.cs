using UnityEngine;
using TMPro;
using System.Linq;

namespace LongLiveKhioyen
{
	public class CommandantOfficeUi : MonoBehaviour
	{
		#region 生命周期
		protected void Start()
		{
			GameInstance.Instance.Data.time.onAdvancedByMonth += OnTimeAdvanced;

			Refresh();
		}

		protected void OnDestroy()
		{
			if(GameInstance.Instance)
			{
				GameInstance.Instance.Data.time.onAdvancedByMonth -= OnTimeAdvanced;
			}
		}

		void Refresh()
		{
			RefreshTime();
			RefreshErp();
		}

		void OnTimeAdvanced(float dt)
		{
			RefreshTime();
		}
		#endregion

		#region 时间
		[Header("Time")]
		[SerializeField] TMP_Text currentTime;

		void RefreshTime()
		{
			currentTime.text = GameInstance.Instance.Data.time.ToLocalizedString();
		}
		#endregion

		#region 预计资源增长
		[Header("ERP")]
		[SerializeField] TMP_Text erpText;
		void RefreshErp()
		{
			erpText.text = string.Join(", ", PolisData.Current.CalculateMonthlyResourceChanges().Select(d => d.ToString()));
		}
		#endregion
	}
}
