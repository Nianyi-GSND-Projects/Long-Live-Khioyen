using Nianyi.UnityPack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
	[Serializable]
	public class GameTime
	{
		#region 定义及转换
		/// <summary>以月计。</summary>
		[SerializeField] float monthsElapsed;

		public static implicit operator float(GameTime gt) => gt.monthsElapsed;

		public static float MonthToGameTime => GameManager.InternalSettings.monthLength;
		/// <summary>游戏开始于公元前 121 年。</summary>
		const float startTime = -121 * 12;
		public static float ElapsedMonthsToAbsolute(float months) => months + startTime;

		public float AbsoluteMonth_Interpolated => ElapsedMonthsToAbsolute(monthsElapsed);
		public int AbsoluteMonth => Mathf.FloorToInt(AbsoluteMonth_Interpolated);
		public float AbsoluteMonth_Frac => AbsoluteMonth_Interpolated - AbsoluteMonth;
		public float Month_Interpolated => MathUtility.Mod(AbsoluteMonth_Interpolated, 12);
		public int Month => Mathf.FloorToInt(Month_Interpolated);
		public int Year => Mathf.FloorToInt(AbsoluteMonth_Interpolated / 12);
		public float ElapsedGameTime => monthsElapsed * MonthToGameTime;
		#endregion

		#region 步进
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
				int estimatedAbsoluteMonth = Mathf.FloorToInt(AbsoluteMonth_Interpolated + dMonth);
				if(estimatedAbsoluteMonth == AbsoluteMonth)
					break;

				float currentDMonth = estimatedAbsoluteMonth - AbsoluteMonth_Interpolated;
				AdvanceNoticedSimple(currentDMonth);
				onMonthPassed?.Invoke();
				dMonth -= currentDMonth;
			}

			if(dMonth > 0)
				AdvanceNoticedSimple(dMonth);
		}

		/// <summary>度时，并触发度时事件。</summary>
		/// <param name="dMonth">以月计。</param>
		void AdvanceNoticedSimple(float dMonth)
		{
			monthsElapsed += dMonth;
			onAdvancedByGameTime?.Invoke(dMonth);
			onAdvancedByMonth?.Invoke(dMonth * MonthToGameTime);
		}
		#endregion

		#region 本地化
		public string ToLocalizedString(string locale = "en")
		{
			return $"{LocalizeMonth(Month, locale)}, {LocalizeAbsoluteYear(Year)}";
		}

		static string[] englishMonths = new string[]
		{ "January", "Febuary", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
		static string[] chineseNumbers = new string[]
		{ "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九",  "十", "十一", "十二", };
		static string ToChineseNumberLiteral(int number)
		{
			if(number <= 0)
				return number == 0 ? chineseNumbers[0] : $"负{ToChineseNumberLiteral(-number)}";

			List<string> digits = new();
			while(number > 0)
			{
				digits.Add(chineseNumbers[number % 10]);
				number /= 10;
			}
			digits.Reverse();
			return string.Join("", digits);
		}

		public static string LocalizeMonth(int month, string locale = "en")
		{
			int i = Mathf.FloorToInt(MathUtility.Mod(month, 12));
			switch(locale)
			{
				case "zh-Hans":
					return $"{chineseNumbers[i]}月";
				case "en":
				default:
					return englishMonths[i];
			}
		}

		public static string LocalizeAbsoluteYear(int year, string locale = "en")
		{
			bool isAd = year >= 0;
			switch(locale)
			{
				case "zh-Hans":
					return isAd ? $"公元 {ToChineseNumberLiteral(year + 1)} 年" : $"公元前 {ToChineseNumberLiteral(-year)} 年";
				case "en":
				default:
					return isAd ? $"{year + 1} AD" : $"{-year} BC";
			}
		}
		#endregion
	}
}
