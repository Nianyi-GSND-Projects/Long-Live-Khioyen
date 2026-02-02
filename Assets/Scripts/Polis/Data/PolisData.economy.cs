using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 资源
		public Action onEconomyChanged;

		[SerializeField] public Economy economy;
		public Economy Economy
		{
			get => economy;
			set
			{
				economy = value;
				onEconomyChanged?.Invoke();
			}
		}

		public bool CheckResourceAffordance(Economy cost)
		{
			return cost <= Economy;
		}

		public bool TryCostResource(Economy cost, bool actuallyCost = true)
		{
			if(!CheckResourceAffordance(cost))
				return false;
			if(actuallyCost)
				Economy -= cost;
			return true;
		}

		public bool CostRecipe(Recipe recipe)
		{
			Economy cost = default;
			foreach(var c in recipe.costs)
			{
				switch(c.type)
				{
					case EconomyType.Food:
						cost.food += c.value;
						break;
					case EconomyType.Material:
						cost.material += c.value;
						break;
					case EconomyType.Money:
						cost.money += c.value;
						break;
				}
			}
			return TryCostResource(cost, true);
		}

		public bool ValidateRecipeCost(Recipe recipe)
		{
			foreach(var cost in recipe.costs)
			{
				switch(cost.type)
				{
					case EconomyType.Food:
						if(Economy.food < cost.value)
							return false;
						break;
					case EconomyType.Material:
						if(Economy.material < cost.value)
							return false;
						break;
					case EconomyType.Money:
						if(Economy.money < cost.value)
							return false;
						break;
					case EconomyType.Population:
						if(FreePopulation < cost.value)
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

		#region 物品
		[Serializable]
		public class ItemRecord
		{
			public string itemId;
			public int quantity;
		}
		public List<ItemRecord> items;

		public void AddItem(string itemId, int quantity)
		{
			var record = items.FirstOrDefault(r => r.itemId == itemId);
			if(record == null)
			{
				record = new() { itemId = itemId, };
				items.Add(record);
			}
			record.quantity += quantity;
		}
		#endregion

		#region 制造
		public List<string> queuedProductions;

		public PolisTask ProductionTask => Tasks.FirstOrDefault(t => t.type == PolisTaskType.itemProduced);
		public bool IsProducingItem => ProductionTask != null;

		public Action onProductionStateChanged;

		public void QueueProduction(Recipe recipe)
		{
			if(!ValidateRecipeCost(recipe))
			{
				Debug.LogWarning($"没有足够多的资源制造 {recipe.item.name}。");
				return;
			}
			CostRecipe(recipe);

			if(IsProducingItem)
				queuedProductions.Add(recipe.item.itemId);
			else
				AddProductionTask(recipe.item.itemId);

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
			AddItem(itemId, 1);
			Debug.Log($"物品 {itemId} 制造完成。");

			if(queuedProductions.Count == 0)
				onProductionStateChanged?.Invoke();
			else
				PerformNextProductionInQueue();
		}
		#endregion
	}

	public class Recipe
	{
		public ItemDefinition item;
		public CostDescriptor[] costs;
	}
}
