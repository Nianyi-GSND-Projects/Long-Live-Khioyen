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

		public List<ResourceDescriptor> MonthlyResourceChanges { get; private set; } = new();
		/// <summary>度月时资源增长。</summary>
		void UpdateResourcesMonthly()
		{
			MonthlyResourceChanges.Clear();
			MonthlyResourceChanges.AddRange(CalculateMonthlyResourceChanges());
			economy.Add(MonthlyResourceChanges);
			NotifyPossiblePopulationChange();
		}

		IEnumerable<ResourceDescriptor> CalculateMonthlyResourceChanges()
		{
			// #### 钱财 ####

			float dMoney = 0;

			// 将驿站寄卖的物品折现
			foreach(var record in forSaleItems)
				dMoney += record.Definition.sellPrice * record.quantity;
			forSaleItems.Clear();

			yield return new() { type = ResourceType.Money, quantity = dMoney, };


			// #### 粮食 ####
			
			yield return new() { type = ResourceType.Food, quantity = 100, };  // TODO: 粮食增长公式


			// #### 人口 ####

			int dPopulation = 2;  // TODO: “理应”的人口增长公式
			if(Population + dPopulation > PopulationCap)
				dPopulation = PopulationCap - Population;

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

		public int Population
		{
			get => population;
			set
			{
				population = Mathf.Min(value, PopulationCap);
				NotifyPossiblePopulationChange();
			}
		}

		public int RequiredPopulation
		{
			get
			{
				if(Tasks.Count == 0)
					return 0;
				return Tasks.Select(t => t.requiredPopulation).Aggregate((a, b) => a + b);
			}
		}

		public int FreePopulation => Population - RequiredPopulation;

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

		// TODO: 应该根据民居与水井的数量及分布计算，但牢宋还没给出具体算法，此处先用 民居数量*10 占位。
		public int PopulationCap
		{
			get
			{
				return QueryBuildingsByTag("dwelling").Length * 10;
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
