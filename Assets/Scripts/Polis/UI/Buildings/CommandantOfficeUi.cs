using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
	public class CommandantOfficeUi : MonoBehaviour
	{
		#region 生命周期
		protected void Start()
		{
			GameInstance.Instance.onGameTimeAdvanced += OnGameTimeAdvanced;

			Refresh();
		}

		protected void OnDestroy()
		{
			if(GameInstance.Instance)
			{
				GameInstance.Instance.onGameTimeAdvanced -= OnGameTimeAdvanced;
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
		[SerializeField] TMP_Text passedTime;

		void OnGameTimeAdvanced(float dt)
		{
			RefreshTime();
		}

		void RefreshTime()
		{
			currentTime.text = Utilities.LocalizeTimeSinceGameStart(GameInstance.Instance.Data.time);
		}
		#endregion
	}
}
