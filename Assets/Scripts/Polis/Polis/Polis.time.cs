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
		}

		void UpdateTime(float dt)
		{
			if(dt > 0)
				GameInstance.Instance.AdvanceTime(dt);
		}

		void FinalizeTime()
		{
			GameInstance.Instance.TimeScale = 1.0f;
		}
	}
}
