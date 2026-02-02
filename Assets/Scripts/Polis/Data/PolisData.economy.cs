using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		public int population;
		public Economy economy;

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

		public List<string> queuedProductions;

		public PolisTask ProductionTask => Tasks.FirstOrDefault(t => t.type == PolisTaskType.completeProduction);
		public bool IsProducingItem => ProductionTask != null;
	}
}
