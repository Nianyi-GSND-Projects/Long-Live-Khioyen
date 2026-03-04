using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
				initialFood = foodAmount,
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
		[Serializable]
		public class PromotionData
		{
			public GameCommander commander;
			public Economy cost;
		}

		// 此值应序列化，否则按照现有的随机生成设计，不能保证每次打开存档都能稳定复现。
		[SerializeField] PromotionData nextPromotion;

		public Action onNextPromotableCommanderChanged;

		/// <remarks>
		/// 在没被外部条件触发导致变化时是幂等的。
		/// </remarks>
		public PromotionData GetNextPromotion()
		{
			// 可能会被 Unity 的序列化设置为非 null 但全空的值
			if(string.IsNullOrEmpty(nextPromotion?.commander?.commanderName))
				nextPromotion = GeneratePromotion();

			return nextPromotion;
		}

		PromotionData GeneratePromotion()
		{
			Economy cost = new(GameManager.InternalSettings.promotionCost);
			PromotionData res = new()
			{
				commander = CommanderRegistry.Instance.GenerateCommander(CommanderGenerationProfile.Default),
				cost = cost,
			};
			return res;
		}

		public void PromoteCommander()
		{
			var promotion = GetNextPromotion();

			if(promotion.commander == null)
			{
				Debug.LogWarning("提拔失败：没有可提拔的指挥官。");
				return;
			}
			if(!Economy.TryCost(promotion.cost, true))
			{
				Debug.LogWarning("提拔失败：资源不足。");
				return;
			}

			// 把当前的加到驻军列表里去
			BattalionStatus battalion = new()
			{
				battalionCommander = promotion.commander,
				currentSolider = 0,
				battalionDefinition = UnitDatabase.BattalionDefinitionSheet.GetUnit(0) as BattalionDefinition,  // TODO: 稳定获取龙鸣
			};
			garrisonedBattalions.Add(battalion);

			Debug.Log($"提拔了武将“{promotion.commander.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotion = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion

		#region 将领
		public void EquipForCommander(GameCommander commander, EquipmentDefinition equipment, int slot)
		{
			if(commander == null || equipment == null || slot < 0 || slot >= (commander.equipments?.Length ?? 0))
				return;

			ResourceDescriptor cost = new()
			{
				type = ResourceType.Item,
				quantity = 1,
				itemId = equipment.itemId,
			};
			if(!Economy.CanCover(cost))
			{
				Debug.LogWarning($"尝试给武将 {commander.commanderName} 装备 {equipment.itemName}，库存不足。");
				return;
			}

			if(commander.equipments[slot] != null)
			{
				Economy.Add(new ResourceDescriptor()
				{
					type = ResourceType.Item,
					quantity = 1,
					itemId = equipment.itemId,
				});
				commander.equipments[slot] = null;
			}

			if(equipment != null)
			{
				Economy.Cost(cost);
				commander.equipments[slot] = equipment;
			}

			Debug.Log($"武将 {commander.commanderName} 在第 {slot} 个槽装备了 {equipment.itemName}。");

			onGarrisonChanged?.Invoke();
		}
		#endregion
	}
}
