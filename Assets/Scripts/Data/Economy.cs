using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		Population = 0x8,
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

		public readonly override string ToString()
		{
			return type switch
			{
				ResourceType.Item => itemId,
				_ => type.GetType().GetEnumName(type),
			} + $"*{quantity}";
		}
	}

	[Serializable]
	public class Economy
	{
		#region 内容
		// 数值资源
		public float food;
		public float material;
		public float money;
		public int population;

		// 物品
		public ItemRecords items = new();
		#endregion

		public Action onChanged;

		#region 基础 IO
		public IEnumerable<ResourceDescriptor> ToDescriptors()
		{
			yield return new ResourceDescriptor() { type = ResourceType.Food, quantity = food, };
			yield return new ResourceDescriptor() { type = ResourceType.Material, quantity = material, };
			yield return new ResourceDescriptor() { type = ResourceType.Money, quantity = money, };
			yield return new ResourceDescriptor() { type = ResourceType.Population, quantity = population, };
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

		public IEnumerable<(ResourceType, ResourceDescriptor)> ToPairs()
		{
			return ToDescriptors().Select(d => (d.type, d));
		}

		public Economy(IEnumerable<ResourceDescriptor> descriptors)
		{
			foreach(var d in descriptors)
				Set(d);
		}

		public Economy(params ResourceDescriptor[] descriptors) : this(descriptors as IEnumerable<ResourceDescriptor>) { }

		public Economy(Economy economy) : this(economy.ToDescriptors()) { }

		public static implicit operator ResourceDescriptor[](Economy economy)
			=> economy.ToDescriptors().ToArray();

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
				case ResourceType.Population:
					population = (int)descriptor.quantity;
					break;
				case ResourceType.Item:
					items.SetItemQuantity(descriptor.itemId, (int)descriptor.quantity);
					break;
				default:
					throw new NotSupportedException($"不支持设置类型为 {descriptor.type} 的资源数量。");
			}
		}

		public float Get(in ResourceDescriptor descriptor)
		{
			return descriptor.type switch
			{
				ResourceType.Food => food,
				ResourceType.Material => material,
				ResourceType.Money => money,
				ResourceType.Population => population,
				ResourceType.Item => items.GetItemQuantity(descriptor.itemId),
				_ => throw new NotSupportedException($"不支持读取类型为 {descriptor.type} 的资源数量。"),
			};
		}

		void ChangeBy(in ResourceDescriptor descriptor)
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
				case ResourceType.Population:
					population += Mathf.RoundToInt(descriptor.quantity);
					break;
				case ResourceType.Item:
					items.ChangeItemQuantity(descriptor.itemId, (int)descriptor.quantity);
					break;
				default:
					throw new NotSupportedException($"不支持更改类型为 {descriptor.type} 的资源数量。");
			}
		}

		public Economy Copy() => new(this);

		public Economy CopyFn(Func<ResourceDescriptor, ResourceDescriptor> fn)
		 => new(ToDescriptors().Select(fn));

		public Economy CopyFn(Func<float, float> fn)
		{
			return CopyFn((ResourceDescriptor d) =>
			{
				d.quantity = fn(d.quantity);
				return d;
			});
		}
		#endregion

		#region 四则运算
		public override bool Equals(object obj)
		{
			if(obj is not Economy)
				return false;
			var b = (Economy)obj;

			foreach(var d in ToDescriptors())
			{
				if(b.Get(d) != d.quantity)
					return false;
			}

			foreach(var d in b.ToDescriptors())
			{
				if(Get(d) != d.quantity)
					return false;
			}

			return true;
		}

		// Make C# happy.
		public override int GetHashCode()
			=> base.GetHashCode();

		public static bool operator >=(in Economy a, in Economy b)
			=> a.CanCover(b);

		public static bool operator <=(in Economy a, in Economy b)
			=> b >= a;

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
		public bool CanCover(params ResourceDescriptor[] costs)
		{
			foreach(var d in costs)
			{
				if(Get(d) < d.quantity)
					return false;
			}

			return true;
		}

		public void Cost(IEnumerable<ResourceDescriptor> costs)
		{
			// 手动循环是因为对于 item 来说负数会被自动截断到 0。
			foreach(var d in costs)
			{
				var cost = d;
				cost.quantity = -d.quantity;
				ChangeBy(cost);
			}

			onChanged?.Invoke();
		}

		public void Cost(params ResourceDescriptor[] costs)
			=> Cost(costs as IEnumerable<ResourceDescriptor>);

		public void Add(IEnumerable<ResourceDescriptor> costs)
		{
			foreach(var d in costs)
				ChangeBy(d);

			onChanged?.Invoke();
		}

		public void Add(params ResourceDescriptor[] costs) => Add(costs as IEnumerable<ResourceDescriptor>);

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
