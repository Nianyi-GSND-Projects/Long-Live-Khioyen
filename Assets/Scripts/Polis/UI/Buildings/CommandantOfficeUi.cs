using UnityEngine;
using TMPro;

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
		}
		#endregion

		#region 时间
		[Header("Time")]
		[SerializeField] TMP_Text currentTime;

		void OnTimeAdvanced(float dt)
		{
			RefreshTime();
		}

		void RefreshTime()
		{
			currentTime.text = GameInstance.Instance.Data.time.ToLocalizedString();
		}
		#endregion
	}
}
