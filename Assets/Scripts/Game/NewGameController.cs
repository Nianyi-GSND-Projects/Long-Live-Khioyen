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

			// 引导战役需要关联到一个敌对（Hostile, type=2）的城池才能进入战斗。
			// 使用数据中的 huns-debug 作为载体，并挂载 tutorial 这个预设战役
			GameData.mainPolis = "juyan";
			GameData.lastPolis = "huns-debug";

			var tutorialPreset = Resources.Load<BattlePresetSO>("PresetBattle/FixedBattle/Tutorial");
			var hunsDebug = GameData.GetPolis("huns-debug");
			if (hunsDebug != null && tutorialPreset != null)
			{
				hunsDebug.fixedBattlePreset = tutorialPreset;
			}

			// 初始化主城驻军和军队状态
			AddStartingGarrison();
			if (Game.CurrentMode != GameInstance.Mode.Polis)
			{
				GameInstance.Instance.ActiveArmy = new()
				{
					armyCommander = null,
					battalionStatuses = new(),
					initialFood = 1000,
					carriedFood = 900,
				};
			}

			// 进入引导战役。
			Debug.Log("进入引导战役。");
			Game.EnterPolis("huns-debug");

			// 等待战役结束。
			bool battleFinished = false;
			System.Action onBattleDone = null;
			onBattleDone = () =>
			{
				battleFinished = true;
				Game.onModeChanged -= onBattleDone;
			};
			Game.onModeChanged += onBattleDone;

			yield return new WaitUntil(() => battleFinished);
			Debug.Log("引导战役结束。");

			// 战役结束后，自动将玩家转入居延主城
			Debug.Log("进入主城：juyan");
			Game.EnterPolis("juyan");
		}

		void AddStartingGarrison()
		{
			var mainPolis = GameData.GetPolis(GameData.mainPolis)
						   ?? GameData.GetPolis(GameData.lastPolis);
			if(mainPolis == null)
			{
				Debug.LogWarning("找不到主城，无法添加初始驻军。");
				return;
			}

			// 若已有驻军则跳过（避免重复添加）
			if(mainPolis.GetGarrisonedCommanders().Count > 0)
				return;

			var changHui = Resources.Load<CommanderTemplateSO>("Data/Starting Commanders/Chang Hui");
			var laiDan   = Resources.Load<CommanderTemplateSO>("Data/Starting Commanders/Lai Dan");
			var zhengJi  = Resources.Load<CommanderTemplateSO>("Data/Starting Commanders/Zheng Ji");

			var db = UnitDatabase.BattalionDefinitionSheet;
			var lightCavalry = db.GetUnit(15) as BattalionDefinition;  // Light Cavalry
			var archers      = db.GetUnit(7)  as BattalionDefinition;  // Archers
			var infantry     = db.GetUnit(14) as BattalionDefinition;  // Infantry

			if(changHui != null && lightCavalry != null)
				mainPolis.AddStartingBattalion(changHui, lightCavalry, 600);
			if(laiDan != null && archers != null)
				mainPolis.AddStartingBattalion(laiDan, archers, 400);
			if(zhengJi != null && infantry != null)
				mainPolis.AddStartingBattalion(zhengJi, infantry, 500);

			Debug.Log($"已为 {mainPolis.id} 添加 3 支初始驻军。");
		}
	}
}
