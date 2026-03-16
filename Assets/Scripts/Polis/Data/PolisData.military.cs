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
			if(battalion == null)
				return;

			targetCount = Mathf.Clamp(targetCount, 0, battalion.MaxSolider);
			int dCount = targetCount - battalion.currentSolider;
			if(dCount == 0)
				return;

			if(dCount > 0)
			{
				if(dCount > FreePopulation)
				{
					Debug.LogWarning($"无法将 {battalion.battalionCommander.commanderName} 的军队人数调整为 {targetCount}：需要 {dCount} 空闲人口，当前空闲人口 {FreePopulation}。");
					return;
				}

				if(!TryCostBattalionWeapons(battalion.battalionDefinition, dCount))
				{
					Debug.LogWarning($"无法将 {battalion.battalionCommander.commanderName} 的军队人数调整为 {targetCount}：对应兵器库存不足。");
					return;
				}
			}
			else
			{
				// 缩编时把对应兵器返还库存。
				ReturnBattalionWeapons(battalion.battalionDefinition, -dCount);
			}

			battalion.currentSolider += dCount;
			Population -= dCount;
			battalion.onChanged?.Invoke();
		}

		/// <summary>
		/// 切换驻军兵种：先返还旧兵器，再按新兵器上限裁剪兵员并扣除新兵器。
		/// </summary>
		public bool TryChangeBattalionDefinition(BattalionStatus battalion, BattalionDefinition targetDefinition)
		{
			if(battalion == null || targetDefinition == null)
				return false;

			if(battalion.battalionDefinition == targetDefinition)
				return true;

			int oldSoldierCount = battalion.currentSolider;

			// 1) 把旧兵种占用的兵器先返还到库存。
			ReturnBattalionWeapons(battalion.battalionDefinition, oldSoldierCount);

			// 2) 计算新兵种可承载上限（人口、编制、新兵器库存）。
			int populationCap = FreePopulation + oldSoldierCount;
			int formationCap = GetMaxSoldierForDefinition(battalion.battalionCommander, targetDefinition);
			int weaponCap = GetWeaponLimitedSoldierCap(targetDefinition, oldSoldierCount);
			int newSoldierCount = Mathf.Clamp(oldSoldierCount, 0, Mathf.Min(populationCap, Mathf.Min(formationCap, weaponCap)));

			// 防御性约束：不允许切兵种后出现负闲置人口。
			int maxByFreePopulation = oldSoldierCount + Mathf.Max(FreePopulation, 0);
			newSoldierCount = Mathf.Min(newSoldierCount, maxByFreePopulation);

			// 3) 扣除新兵种需要占用的兵器。
			if(!TryCostBattalionWeapons(targetDefinition, newSoldierCount))
			{
				// 理论上不会到这里（上面已按 weaponCap 裁剪），留保护并回滚旧兵器。
				TryCostBattalionWeapons(battalion.battalionDefinition, oldSoldierCount);
				Debug.LogWarning($"切换 {battalion.battalionCommander?.commanderName} 的兵种失败：新兵器库存不足。");
				return false;
			}

			// 4) 应用兵种与兵员变更。
			battalion.battalionDefinition = targetDefinition;
			int dCount = newSoldierCount - oldSoldierCount;
			battalion.currentSolider = newSoldierCount;
			Population -= dCount;
			battalion.onChanged?.Invoke();
			return true;
		}

		int GetMaxSoldierForDefinition(GameCommander commander, BattalionDefinition definition)
		{
			if(definition == null)
				return 0;

			int maxSoldier = definition.defaultMaxSolider;
			if(commander != null)
				maxSoldier += commander.GetMaxSoldiersBonus();
			return Mathf.Max(maxSoldier, 0);
		}

		int GetWeaponLimitedSoldierCap(BattalionDefinition definition, int currentSoldierCount)
		{
			string requiredWeaponId = GetRequiredWeaponItemId(definition);
			if(string.IsNullOrEmpty(requiredWeaponId))
				return int.MaxValue;

			int stock = StockedItems.GetItemQuantity(requiredWeaponId);
			return currentSoldierCount + Mathf.Max(stock, 0);
		}

		string GetRequiredWeaponItemId(BattalionDefinition definition)
		{
			if(definition?.requiredWeapon == null)
				return null;

			if(string.IsNullOrEmpty(definition.requiredWeapon.itemId))
				return string.Empty;

			return definition.requiredWeapon.itemId;
		}

		bool TryCostBattalionWeapons(BattalionDefinition definition, int count)
		{
			if(count <= 0)
				return true;

			string requiredWeaponId = GetRequiredWeaponItemId(definition);
			if(requiredWeaponId == null)
				return true;

			if(requiredWeaponId == string.Empty)
				return false;

			ResourceDescriptor cost = new()
			{
				type = ResourceType.Item,
				itemId = requiredWeaponId,
				quantity = count,
			};
			if(!Economy.CanCover(cost))
				return false;

			Economy.Cost(cost);
			return true;
		}

		void ReturnBattalionWeapons(BattalionDefinition definition, int count)
		{
			if(count <= 0)
				return;

			string requiredWeaponId = GetRequiredWeaponItemId(definition);
			if(string.IsNullOrEmpty(requiredWeaponId))
				return;

			Economy.Add(new ResourceDescriptor()
			{
				type = ResourceType.Item,
				itemId = requiredWeaponId,
				quantity = count,
			});
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
				battalionDefinition = GameManager.InternalSettings.defaultBattalionType,
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

		#region 训练
		public IEnumerable<BattalionDefinition> GetTrainableBattalions()
		{
			return UnitDatabase.BattalionDefinitionSheet.unitDefinitions.OfType<BattalionDefinition>()
				.Where(d => d.isTrainable && HasBuildingsWithTags(d.requiredBuildingTags));
		}
		#endregion
	}
}
