using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 经济资源种类描述符，主要用于方便地指代几种预定义的资源。
	/// </summary>
	[Serializable]
	public enum ResourceType
	{
		Undefined = 0b0,
		Food = 0x4, Material, Money,
		Item = 0x10,
		Custom = 0xffff,
	}

	[Serializable]
	public struct ResourceDescriptor
	{
		public ResourceType type;
		/// <summary>只有在 type 为 Item 时有效。</summary>
		public string itemId;

		public float quantity;
	}

	[Serializable]
	public struct Economy
	{
		#region 内容
		// 数值资源
		public float food;
		public float material;
		public float money;

		// 物品
		public ItemRecords items;
		#endregion

		#region 基础 IO
		public readonly IEnumerable<ResourceDescriptor> ToDescriptors()
		{
			yield return new ResourceDescriptor() { type = ResourceType.Food, quantity = food, };
			yield return new ResourceDescriptor() { type = ResourceType.Material, quantity = material, };
			yield return new ResourceDescriptor() { type = ResourceType.Money, quantity = money, };
			foreach(var record in items)
			{
				yield return new ResourceDescriptor()
				{
					type = ResourceType.Item,
					itemId = record.itemId,
					quantity = record.quantity,
				};
			}
		}

		public readonly IEnumerable<(ResourceType, ResourceDescriptor)> ToPairs()
		{
			return ToDescriptors().Select(d => (d.type, d));
		}

		public Economy(IEnumerable<ResourceDescriptor> descriptors)
		{
			food = default;
			material = default;
			money = default;
			items = new();

			foreach(var d in descriptors)
				Set(d);
		}

		public Economy(params ResourceDescriptor[] descriptors) : this(descriptors as IEnumerable<ResourceDescriptor>) { }

		public Economy(Economy economy) : this(economy.ToDescriptors()) { }

		public void Set(in ResourceDescriptor descriptor)
		{
			switch(descriptor.type)
			{
				case ResourceType.Food:
					food = descriptor.quantity;
					break;
				case ResourceType.Material:
					material = descriptor.quantity;
					break;
				case ResourceType.Money:
					money = descriptor.quantity;
					break;
				case ResourceType.Item:
					items.SetItemQuantity(descriptor.itemId, (int)descriptor.quantity);
					break;
				default:
					throw new NotSupportedException($"不支持设置类型为 {descriptor.type} 的资源数量。");
			}
		}

		public readonly float Get(in ResourceDescriptor descriptor)
		{
			switch(descriptor.type)
			{
				case ResourceType.Food:
					return descriptor.quantity;
				case ResourceType.Material:
					return descriptor.quantity;
				case ResourceType.Money:
					return descriptor.quantity;
				case ResourceType.Item:
					return items.GetItemQuantity(descriptor.itemId);
				default:
					throw new NotSupportedException($"不支持独去类型为 {descriptor.type} 的资源数量。");
			}
		}

		public void ChangeBy(in ResourceDescriptor descriptor)
		{
			switch(descriptor.type)
			{
				case ResourceType.Food:
					food += descriptor.quantity;
					break;
				case ResourceType.Material:
					material += descriptor.quantity;
					break;
				case ResourceType.Money:
					money += descriptor.quantity;
					break;
				case ResourceType.Item:
					items.ChangeItemQuantity(descriptor.itemId, (int)descriptor.quantity);
					break;
				default:
					throw new NotSupportedException($"不支持更改类型为 {descriptor.type} 的资源数量。");
			}
		}

		public void ChangeBy(in Economy delta)
		{
			foreach(var d in delta.ToDescriptors())
				ChangeBy(d);
		}

		public readonly Economy Copy() => new(this);

		public readonly Economy CopyFn(Func<ResourceDescriptor, ResourceDescriptor> fn)
		 => new(ToDescriptors().Select(fn));

		public readonly Economy CopyFn(Func<float, float> fn)
		{
			return CopyFn((ResourceDescriptor d) =>
			{
				d.quantity = fn(d.quantity);
				return d;
			});
		}
		#endregion

		#region 四则运算
		/// <remarks>AI generated</remarks>
		public static bool operator ==(in Economy a, in Economy b)
		{
			// 将两个 Economy 对象转换为 Dictionary，比较键值是否完全相同
			var dictA = a.ToDescriptors().GroupBy(d => (d.type, d.itemId))
				.ToDictionary(g => g.Key, g => g.First().quantity);
			var dictB = b.ToDescriptors().GroupBy(d => (d.type, d.itemId))
				.ToDictionary(g => g.Key, g => g.First().quantity);

			// 如果键的个数不同，则不相等
			if(dictA.Count != dictB.Count)
				return false;

			// 比较所有键值
			foreach(var kvp in dictA)
			{
				if(!dictB.TryGetValue(kvp.Key, out var valueB) || !kvp.Value.Equals(valueB))
					return false;
			}

			return true;
		}

		public static bool operator !=(in Economy a, in Economy b)
			=> !(a == b);

		/// <remarks>AI generated</remarks>
		public static bool operator <(in Economy a, in Economy b)
		{
			// a < b 当且仅当 a 的所有资源都小于等于 b，且至少有一个资源严格小于 b
			bool anyLess = false;

			foreach(var descriptorA in a.ToDescriptors())
			{
				var quantityB = b.Get(descriptorA);
				if(descriptorA.quantity > quantityB)
					return false; // 发现有一个资源 a 大于 b，不满足条件
				if(descriptorA.quantity < quantityB)
					anyLess = true; // 发现至少有一个资源 a 小于 b
			}

			// 还需要检查 b 中是否有 a 中不存在的资源（都视为 0）
			foreach(var descriptorB in b.ToDescriptors())
			{
				var quantityA = a.Get(descriptorB);
				if(quantityA == 0 && descriptorB.quantity > 0)
					anyLess = true; // b 中有 a 没有的资源
			}

			return anyLess;
		}

		/// <remarks>AI generated</remarks>
		public static bool operator >(in Economy a, in Economy b)
		{
			// a > b 当且仅当 b < a
			return b < a;
		}

		public static bool operator <=(in Economy a, in Economy b)
			=> a < b || a == b;

		public static bool operator >=(in Economy a, in Economy b)
			=> a > b || a == b;

		public static Economy operator *(in Economy economy, float scalar)
		{
			return economy.CopyFn(x => x * scalar);
		}

		public static Economy operator +(in Economy a, Economy b)
		{
			return a.CopyFn((ResourceDescriptor d) =>
			{
				d.quantity += b.Get(d);
				return d;
			});
		}

		public static Economy operator -(in Economy a, Economy b)
		{
			return a.CopyFn((ResourceDescriptor d) =>
			{
				d.quantity -= b.Get(d);
				return d;
			});
		}
		#endregion

		#region 基础操作
		public readonly bool CanCover(in Economy cost)
		{
			return cost <= this;
		}

		public void Cost(in Economy delta)
		{
			// 手动循环是因为对于 item 来说负数会被自动截断到 0。
			foreach(var d in delta.ToDescriptors())
			{
				var cost = d;
				cost.quantity = -d.quantity;
				ChangeBy(cost);
			}
		}

		public bool TryCost(in Economy cost, bool actuallyCost = true)
		{
			if(!CanCover(cost))
				return false;
			if(actuallyCost)
				Cost(cost);
			return true;
		}
		#endregion
	}
}
