using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 新游戏的控制器，主要用于引导教程。流程走完后自毁。
	/// </summary>
	public class NewGameController : GameController
	{
		protected override IEnumerator Routine()
		{
			Debug.Log("开启新游戏。");

			// 进入引导战役。
			Debug.Log("进入引导战役。");
			Game.EnterPolis(GameData.lastPolis);

			// 等待战役结束。
			bool battleFinished = false;
			Game.onModeChanged.Once(() => battleFinished = false);
			yield return new WaitUntil(() => battleFinished);
			Debug.Log("引导战役结束。");

			// TODO: 后续步骤
		}
	}
}
