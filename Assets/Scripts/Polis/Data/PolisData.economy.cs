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

		public Action onEconomyChanged;

		public bool CheckResourceAffordance(Economy cost)
		{
			return Economy.CanCover(cost);
		}

		public bool TryCostResource(Economy cost, bool actuallyCost = true)
		{
			if(!CheckResourceAffordance(cost))
				return false;
			if(actuallyCost)
				Economy.Cost(cost);
			return true;
		}

		public bool CostByDescriptor(IEnumerable<ResourceDescriptor> costs)
		{
			Economy cost = default;
			foreach(var c in costs)
			{
				switch(c.type)
				{
					case ResourceType.Food:
						cost.food += c.quantity;
						break;
					case ResourceType.Material:
						cost.material += c.quantity;
						break;
					case ResourceType.Money:
						cost.money += c.quantity;
						break;
				}
			}
			return TryCostResource(cost, true);
		}
		public bool CostByDescriptor(params ResourceDescriptor[] costs) => CostByDescriptor(costs as IEnumerable<ResourceDescriptor>);

		public bool ValidateRecipeCost(IEnumerable<ResourceDescriptor> costs)
		{
			foreach(var cost in costs)
			{
				switch(cost.type)
				{
					case ResourceType.Food:
						if(Economy.food < cost.quantity)
							return false;
						break;
					case ResourceType.Material:
						if(Economy.material < cost.quantity)
							return false;
						break;
					case ResourceType.Money:
						if(Economy.money < cost.quantity)
							return false;
						break;
				}
			}
			return true;
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
			private set
			{
				population = Mathf.Min(value, PopulationCap);
				onPopulationDataChanged?.Invoke();
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

			if(!ValidateRecipeCost(item.costs))
			{
				Debug.LogWarning($"没有足够多的资源制造 {itemId}。");
				return;
			}
			CostByDescriptor(item.costs);

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

		/// <summary>
		/// 月尾将驿站寄卖的物品折现。
		/// </summary>
		void CashForSaleItemsAtEndOfMonth()
		{
			float sum = 0;
			foreach(var itemDefinition in forSaleItems.Definitions)
				sum += itemDefinition.sellPrice;

			forSaleItems.Clear();
			economy.money += sum;
			onEconomyChanged?.Invoke();
		}
		#endregion
	}
}
