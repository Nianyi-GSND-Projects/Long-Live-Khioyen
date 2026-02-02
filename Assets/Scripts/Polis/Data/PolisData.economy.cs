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

		public void QueueProduction(string itemId)
		{
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
			AddItem(itemId, 1);
			Debug.Log($"物品 {itemId} 制造完成。");
			onProductionStateChanged?.Invoke();

			PerformNextProductionInQueue();
		}
		#endregion
	}
}
