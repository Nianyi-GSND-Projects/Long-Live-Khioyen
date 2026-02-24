using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 驻扎
		[SerializeField] List<BattalionStatus> garrisonedBattalions = new();

		public int SoliderCount
			=> garrisonedBattalions.Select(b => b.currentSolider).Aggregate(0, (a, b) => a + b);

		public Action onGarrisonChanged;

		public IReadOnlyList<GameCommander> GetGarrisonedCommanders()
			=> garrisonedBattalions.Select(b => b.battalionCommander).ToList();

		public BattalionStatus GetGarrisonedBattalionByCommander(GameCommander commander)
			=> garrisonedBattalions.Find(b => b.battalionCommander == commander);

		/// <summary>使军队出城。</summary>
		/// <param name="headCommander">暂时不用。</param>
		public ArmyStatus LetOutGarrison(GameCommander headCommander, float foodAmount, IReadOnlyList<GameCommander> battalionCommanders)
		{
			// 移除城中军队
			var battalions = garrisonedBattalions.Where(b => battalionCommanders.Contains(b.battalionCommander)).ToList();
			foreach(var b in battalions)
				garrisonedBattalions.Remove(b);

			// 取出食物
			Economy.Cost(new ResourceDescriptor()
			{
				type = ResourceType.Food,
				quantity = foodAmount,
			});

			ArmyStatus army = new()
			{
				armyCommander = null,
				battalionStatuses = battalions,
				carriedFood = foodAmount,
			};
			return army;
		}

		/// <summary>使军队驻扎到城内。</summary>
		public void GarrisonArmy(ArmyStatus army)
		{
			if(army == null)
				return;

			// 添加军队到城中
			garrisonedBattalions.AddRange(army.battalionStatuses);

			// 归置资源
			// TODO

			onGarrisonChanged?.Invoke();
		}

		public void SetBattalionSoldierCount(BattalionStatus battalion, int targetCount)
		{
			int dCount = targetCount - battalion.currentSolider;
			Debug.Log($"尝试将 {battalion.battalionCommander.commanderName} 的军队人数调整为 {targetCount}：当前拥军数 {battalion.currentSolider}，需要 {FreePopulation - dCount} 空闲人口，当前空闲人口 {FreePopulation}。");
			if(dCount > FreePopulation)
			{
				Debug.LogWarning($"无法将 {battalion.battalionCommander.commanderName} 的军队人数调整为 {targetCount}：需要 {FreePopulation - dCount} 空闲人口，当前空闲人口 {FreePopulation}。");
				return;
			}
			battalion.currentSolider += dCount;
			Population -= dCount;
			battalion.onChanged?.Invoke();
		}
		#endregion

		#region 提拔
		// 此值应序列化，否则按照现有的随机生成设计，不能保证每次打开存档都能稳定复现。
		[SerializeField] GameCommander nextPromotableCommander;

		public Action onNextPromotableCommanderChanged;

		/// <remarks>
		/// 在没被外部条件触发导致变化时是幂等的。
		/// </remarks>
		public GameCommander GetPromotableCommander()
		{
			// nextPromotableCommander 可能会被 Unity 的序列化设置为非 null 但全空的值
			if(nextPromotableCommander == null || string.IsNullOrEmpty(nextPromotableCommander.commanderName))
				nextPromotableCommander = CommanderRegistry.Instance.GenerateCommander(CommanderGenerationProfile.Default);

			return nextPromotableCommander;
		}

		public void PromoteCommander()
		{
			GameCommander promotedCommander = GetPromotableCommander();

			if(promotedCommander == null)
			{
				Debug.LogWarning("提拔失败：没有可提拔的指挥官。");
				return;
			}

			// 把当前的加到驻军列表里去
			BattalionStatus battalion = new()
			{
				battalionCommander = promotedCommander,
				currentSolider = 0,
				battalionDefinition = UnitDatabase.BattalionDefinitionSheet.GetUnit(0) as BattalionDefinition,  // 龙鸣
			};
			garrisonedBattalions.Add(battalion);

			Debug.Log($"提拔了武将“{promotedCommander.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotableCommander = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion
	}
}
