using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 资源
		[SerializeField] public Economy economy;
		public ItemRecords StockedItems => economy.items;
		public Economy Economy
		{
			get => economy;
			set
			{
				economy = value;
				onEconomyChanged?.Invoke();
			}
		}

		public Action onEconomyChanged
		{
			get => Economy.onChanged;
			set => Economy.onChanged = value;
		}

		List<ResourceDescriptor> monthlyResourceChanges;
		/// <summary>给月度更迭 UI 提供信息源。</summary>
		public List<ResourceDescriptor> MonthlyResourceChanges
		{
			get
			{
				if(monthlyResourceChanges == null)
					monthlyResourceChanges = new();
				return monthlyResourceChanges;
			}
		}
		/// <summary>度月时资源增长。</summary>
		void UpdateResourcesMonthly()
		{
			// 应用变动
			MonthlyResourceChanges.Clear();
			MonthlyResourceChanges.AddRange(CalculateMonthlyResourceChanges());
			economy.Add(MonthlyResourceChanges);

			// 清空寄卖物品
			forSaleItems.Clear();

			NotifyPossiblePopulationChange();
		}

		public IEnumerable<ResourceDescriptor> CalculateMonthlyResourceChanges()
		{
			// #### 钱财 ####

			float dMoney = 0;

			// 驿站寄卖
			foreach(var record in forSaleItems)
				dMoney += record.Definition.sellPrice * record.quantity;

			// 税收
			dMoney += FreePopulation * 2 + RequiredPopulation * 5 - SoliderCount * 5;

			yield return new() { type = ResourceType.Money, quantity = dMoney, };


			// #### 粮食与人口 ####
			float dFood = 0;
			int dPopulation = 0;

			// 每个粮仓提供 300 粮/月；每居民吃 1 粮/月。
			dFood += QueryBuildingsByTag("granary").Length * 300;
			dFood -= FreePopulation * 10 + RequiredPopulation * 20 + SoliderCount * 20;

			if(dFood > -Economy.food)  // 粮食足抵消耗
			{
				float overshoot = Economy.food + dFood * 3;  // 当前粮草与未来三月预计消耗相比的盈余
				if(overshoot > 0)  // 粮食充足，人口可增长
				{
					// 把 (0, +∞) 的盈余映射到 [10, 30] 上作为增长量
					float t = 1 - 1 / (overshoot + 1);
					dPopulation += Mathf.FloorToInt(Mathf.Lerp(10, 30, t));
					dPopulation = Mathf.Min(dPopulation, PopulationCap - Population);  // 人口上限
				}
			}
			else  // 粮食不抵消耗
			{
				float overshoot = -(Economy.food + dFood);  // 欠这么多粮食
				dFood = -Economy.food;
				dPopulation -= Mathf.CeilToInt(overshoot / 10);  // TODO: 临时的人口衰减公式，每欠 10 粮减一人
				dPopulation = Mathf.Max(-FreePopulation, dPopulation);  // 不能减成负的
			}

			yield return new() { type = ResourceType.Food, quantity = dFood, };
			yield return new() { type = ResourceType.Population, quantity = dPopulation, };
		}
		#endregion

		#region 人口
		[SerializeField] int population;

		public Action onPopulationDataChanged;

		void NotifyPossiblePopulationChange()
		{
			onPopulationDataChanged?.Invoke();
		}

		/// <summary>总人口（不算转换为士兵的）。</summary>
		public int Population
		{
			get => population;
			set
			{
				population = Mathf.Min(value, PopulationCap);
				NotifyPossiblePopulationChange();
			}
		}

		/// <summary>当前人口占用。</summary>
		public int RequiredPopulation => TaskPopulation + PersistentPopulation;

		/// <summary>执行中的任务的人口占用。</summary>
		public int TaskPopulation
			=> Tasks.Select(t => t.requiredPopulation).Aggregate(0, (a, b) => a + b);

		/// <summary>建筑物固有的人口占用。</summary>
		public int PersistentPopulation
			=> buildings.Where(b => !b.underConstruction).Select(b => b.Definition.persistentPopulation).Aggregate(0, (a, b) => a + b);

		/// <summary>当前可用人口。</summary>
		public int FreePopulation => Population - RequiredPopulation;

		/// <summary>当前工作效率。</summary>
		public float Efficiency
		{
			get
			{
				int required = RequiredPopulation;
				if(required <= Population)
					return 1f;
				return (float)Population / required;
			}
		}

		/// <summary>当前人口上限。</summary>
		public int PopulationCap
		{
			get
			{
				// 基础50 + 水井*5 + 住房*20
				var dwellings = QueryBuildingsByTag("dwelling");
				var waterWells = QueryBuildingsByTag("water-well");
				return 50 + dwellings.Length * 20 + waterWells.Length * 5;
			}
		}
		#endregion

		#region 制造
		public List<string> queuedProductions;

		public PolisTask ProductionTask => Tasks.FirstOrDefault(t => t.type == PolisTaskType.itemProduced);
		public bool IsProducingItem => ProductionTask != null;

		public Action onProductionStateChanged;

		public void QueueProduction(string itemId)
		{
			var item = ItemDatabase.Instance.GetItem(itemId);
			if(item == null)
			{
				Debug.LogWarning($"无法生产 ID 为 {itemId} 的物品：无效 ID。");
				return;
			}

			if(!Economy.CanCover(item.costs))
			{
				Debug.LogWarning($"没有足够多的资源制造 {itemId}。");
				return;
			}
			Economy.Cost(item.costs);

			if(IsProducingItem)
				queuedProductions.Add(itemId);
			else
				AddProductionTask(itemId);

			onProductionStateChanged?.Invoke();
		}

		public void PerformNextProductionInQueue()
		{
			if(queuedProductions.Count == 0)
				return;

			var itemId = queuedProductions[0];
			queuedProductions.RemoveAt(0);
			AddProductionTask(itemId);

			onProductionStateChanged?.Invoke();
		}

		void AddProductionTask(string itemId)
		{
			float productionTime = 10f;  // TODO: 得到此种物体的制造时间。
			PolisTask task = new(
				PolisTaskType.itemProduced,
				productionTime,
				itemId
			);
			AddTask(task);
			Debug.Log($"开始制造物品：{itemId}。");
		}

		void ExecuteCompleteProductionTask(PolisTask task)
		{
			string itemId = task.parameters[0];
			StockedItems.ChangeItemQuantity(itemId, 1);
			Debug.Log($"物品 {itemId} 制造完成。");

			if(queuedProductions.Count == 0)
				onProductionStateChanged?.Invoke();
			else
				PerformNextProductionInQueue();
		}
		#endregion

		#region 交易
		public ItemRecords forSaleItems;

		public void SetItemForSale(string itemId, int quantity)
		{
			if(!TransferItemRecord(itemId, quantity, StockedItems, forSaleItems))
				Debug.LogWarning($"尝试寄卖 {quantity} 个 {itemId} 失败。");
		}

		public void UnsetItemForSale(string itemId, int quantity)
		{
			if(!TransferItemRecord(itemId, quantity, forSaleItems, StockedItems))
				Debug.LogWarning($"尝试取消寄卖 {quantity} 个 {itemId} 失败。");
		}

		bool TransferItemRecord(string itemId, int quantity, ItemRecords from, ItemRecords to)
		{
			if(quantity <= 0)
				return false;

			var record = from.FirstOrDefault(r => r.itemId == itemId);
			if(record == null)
				return false;

			if(record.quantity < quantity)
				quantity = record.quantity;

			from.ChangeItemQuantity(itemId, -quantity);
			to.ChangeItemQuantity(itemId, quantity);
			return true;
		}
		#endregion
	}
}
