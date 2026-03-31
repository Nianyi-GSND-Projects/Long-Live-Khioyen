using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

namespace LongLiveKhioyen
{
	public class DebugGameController : GameController
	{
		protected override IEnumerator Routine()
		{
			Debug.Log("启动 debug 游戏。");

			// 为主城添加初始驻军
			AddStartingGarrison();

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
				mainPolis.AddStartingBattalion(changHui, lightCavalry, 300);
			if(laiDan != null && archers != null)
				mainPolis.AddStartingBattalion(laiDan, archers, 200);
			if(zhengJi != null && infantry != null)
				mainPolis.AddStartingBattalion(zhengJi, infantry, 250);

			Debug.Log($"已为 {mainPolis.id} 添加 3 支初始驻军。");
		}
	}
}
