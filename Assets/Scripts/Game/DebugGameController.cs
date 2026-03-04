using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace LongLiveKhioyen
{
	public class DebugGameController : GameController
	{
		protected override IEnumerator Routine()
		{
			Debug.Log("启动 debug 游戏。");

			// 城外启动，须填充 ActiveArmy
			if(Game.CurrentMode != GameInstance.Mode.Polis)
			{
				GameInstance.Instance.ActiveArmy = new()
				{
					armyCommander = null,
					battalionStatuses = new(),
					initialFood = 1000,
					carriedFood = 900,
				};
			}

			yield break;
		}
	}
}
