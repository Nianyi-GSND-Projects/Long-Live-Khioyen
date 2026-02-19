using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	[Serializable]
	public class ItemRecord
	{
		public string itemId;
		public int quantity;

		public ItemDefinition Definition => ItemDatabase.Instance.GetItem(itemId);
	}

	[Serializable]
	public class ItemRecords : IList<ItemRecord>, IReadOnlyList<ItemRecord>
	{
		[SerializeField] List<ItemRecord> records;
		public Action onChanged;

		#region 接口
		public void SetItemQuantity(string itemId, int quantity)
		{
			var record = GetRecord(itemId);
			if(record == null)
			{
				record = new() { itemId = itemId, };
				records.Add(record);
			}
			record.quantity = quantity;
			if(record.quantity <= 0)
				records.Remove(record);

			onChanged?.Invoke();
		}

		public int GetItemQuantity(string itemId)
		{
			return GetRecord(itemId)?.quantity ?? 0;
		}

		public void ChangeItemQuantity(string itemId, int quantity)
		{
			if(quantity == 0)
				return;

			var record = GetRecord(itemId);
			if(record == null)
			{
				record = new() { itemId = itemId, };
				records.Add(record);
			}

			if(quantity < 0 && record.quantity + quantity < 0)
				Debug.LogWarning($"现有 {itemId} {record.quantity} 个，欲减少 {-quantity} 个，溢出归零。");
			record.quantity += quantity;
			if(record.quantity <= 0)
				records.Remove(record);

			onChanged?.Invoke();
		}

		public IEnumerable<string> Ids => records.Select(r => r.itemId);
		public IEnumerable<ItemDefinition> Definitions => Ids.Select(id => ItemDatabase.Instance.GetItem(id));

		public ItemRecord GetRecord(string itemId)
		{
			return records.FirstOrDefault(r => r.itemId == itemId);
		}
		public ItemRecord GetRecord(ItemDefinition definition)
			=> GetRecord(definition.itemId);
		#endregion

		#region IList 接口
		public ItemRecord this[int index] { get => ((IList<ItemRecord>)records)[index]; set => ((IList<ItemRecord>)records)[index] = value; }

		public int Count => ((ICollection<ItemRecord>)records).Count;

		public bool IsReadOnly => ((ICollection<ItemRecord>)records).IsReadOnly;

		public void Add(ItemRecord item)
		{
			((ICollection<ItemRecord>)records).Add(item);
			onChanged?.Invoke();
		}

		public void Clear()
		{
			((ICollection<ItemRecord>)records).Clear();
			onChanged?.Invoke();
		}

		public bool Contains(ItemRecord item)
		{
			return ((ICollection<ItemRecord>)records).Contains(item);
		}

		public void CopyTo(ItemRecord[] array, int arrayIndex)
		{
			((ICollection<ItemRecord>)records).CopyTo(array, arrayIndex);
		}

		public IEnumerator<ItemRecord> GetEnumerator()
		{
			return ((IEnumerable<ItemRecord>)records).GetEnumerator();
		}

		public int IndexOf(ItemRecord item)
		{
			return ((IList<ItemRecord>)records).IndexOf(item);
		}

		public void Insert(int index, ItemRecord item)
		{
			((IList<ItemRecord>)records).Insert(index, item);
			onChanged?.Invoke();
		}

		public bool Remove(ItemRecord item)
		{
			var result = ((ICollection<ItemRecord>)records).Remove(item);
			onChanged?.Invoke();
			return result;
		}

		public void RemoveAt(int index)
		{
			((IList<ItemRecord>)records).RemoveAt(index);
			onChanged?.Invoke();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)records).GetEnumerator();
		}
		#endregion
	}
}
