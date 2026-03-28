using UnityEngine;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
	[CreateAssetMenu(menuName = "Long Live Khioyen/Internal Game Settings")]
	public class InternalGameSettings : ScriptableObject
	{
		[Header("时间")]
		[Tooltip("游戏中一逻辑月对应的游戏时间（秒）。")]
		[Label("月时长（秒）"), Min(0)] public float monthLength = 1;

		[Header("税收")]
		[Label("闲置人口税率"), Min(0)] public float freePopulationTaxRate = 1;
		[Label("就业人口税率"), Min(0)] public float busyPopulationTaxRate = 1;
		[Label("士兵费率"), Min(0)] public float soldierCostRate = 1;

		[Header("粮食与人口")]
		[Label("城市基础人口承载量"), Min(0)] public int polisBasicPopulationCapacity = 0;
		[Label("民居人口承载量"), Min(0)] public int dwellingPopulationCapacity = 1;
		[Label("水井人口承载量"), Min(0)] public int waterWellPopulationCapacity = 1;
		[Label("粮仓供粮"), Min(0)] public float foodPerGranary = 1;
		[Label("闲置人口耗粮"), Min(0)] public float freePopulationFoodCost = 1;
		[Label("就业人口耗粮"), Min(0)] public float busyPopulationFoodCost = 1;
		[Label("士兵耗粮"), Min(0)] public float soldierFoodCost = 1;
		[Tooltip("要想使人口增长，在月底扣完耗粮后，城中至少有接下来 n 个月的余粮。")]
		[Label("人口增长最低月粮"), Min(0)] public int minMonthFoodForPopulationGrowth = 1;
		[Label("人口增长范围"), Min(0)] public Vector2Int populationGrowthRange = new(0, 1);
		[Tooltip("余粮耗尽后，每欠多少粮食减一人口。")]
		[Label("人口衰减粮比"), Min(0)] public int populationDecreasePerFood = 1;

		[Header("军备")]
		[Label("武将招募成本")] public Economy promotionCost;
		[Label("默认兵种")] public BattalionDefinition defaultBattalionType;

		[Header("行军")]
		[Label("默认敌方城池图标")] public Sprite fallbackHostileIcon;
		[Label("默认友方城池图标")] public Sprite fallbackFriendlyIcon;
		[Label("行军耗粮速率（每距离每重量）"), Min(0)] public float worldMapFoodCostRate;
		[Label("行军时间流速"), Min(0)] public float worldMapTimeScale;
		[Label("暗雷率（每距离每难度）"), Range(0, 1)] public float encounterRate = 0.001f;

		[Header("战斗")]
		[Label("单次战斗耗粮（测试）"), Min(0)] public float foodCostPerBattle;  // 测试用
		[Label("单次战斗耗时（测试）"), Min(0)] public float timeCostPerBattle;  // 测试用

		[Header("UI")]
		[Label("UI 提示显示延迟"), Min(0)] public float tooltipDelay = 1;
	}
}
