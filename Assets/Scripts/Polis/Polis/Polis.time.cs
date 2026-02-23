using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		void InitializeTime()
		{
			GameInstance.Instance.TimeScale = 1.0f;

			// 度过累积的时间
			float passedTime = GameInstance.Instance.Data.time.ElapsedGameTime - Data.lastTime.ElapsedGameTime;
			Data.PassTime(passedTime);
			GameInstance.Instance.Data.time.onAdvancedByMonth += Data.PassTime;
		}

		void UpdateTime(float dt)
		{
			if(dt > 0)
				GameInstance.Instance.AdvanceTime_Scaled(dt);
		}

		void FinalizeTime()
		{
			if(GameInstance.Instance)
			{
				GameInstance.Instance.Data.time.onAdvancedByMonth -= Data.PassTime;
				GameInstance.Instance.TimeScale = 1.0f;
			}
		}
	}
}
