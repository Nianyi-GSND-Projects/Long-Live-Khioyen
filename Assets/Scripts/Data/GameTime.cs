using UnityEngine;
using System;

namespace LongLiveKhioyen
{
	[Serializable]
	public class GameTime
	{
		#region 定义及转换
		/// <summary>以月计。</summary>
		[SerializeField] float months_Interpolated;

		public static implicit operator float(GameTime gt) => gt.months_Interpolated;

		static float MonthToGameTime => GameManager.InternalSettings.monthLength;
		public float CurrentMonth_Interpolated => months_Interpolated;
		public int CurrentMonth => (int)months_Interpolated;
		#endregion

		public Action<float> onAdvancedByMonth;
		public Action<float> onAdvancedByGameTime;
		public Action onMonthPassed;

		/// <summary>以游戏时间度时。</summary>
		public void AdvanceByInGameTime(float dGt)
		{
			AdvanceByMonth(dGt / MonthToGameTime);
		}

		public void AdvanceByMonth(float dMonth)
		{
			while(true)
			{
				int estimatedMonth = (int)(months_Interpolated + dMonth);
				if(estimatedMonth == CurrentMonth)
					break;

				float currentDMonth = estimatedMonth - months_Interpolated;
				AdvanceNoticedSimple(currentDMonth);
				onMonthPassed?.Invoke();
				months_Interpolated = estimatedMonth;  // 防止浮点误差
				dMonth -= currentDMonth;
			}

			if(dMonth > 0)
				AdvanceNoticedSimple(dMonth);
		}

		/// <summary>度时，并触发度时事件。</summary>
		/// <param name="dMonth">以月计。</param>
		void AdvanceNoticedSimple(float dMonth)
		{
			months_Interpolated += dMonth;
			onAdvancedByGameTime?.Invoke(dMonth);
			onAdvancedByMonth?.Invoke(dMonth * MonthToGameTime);
		}
	}
}
