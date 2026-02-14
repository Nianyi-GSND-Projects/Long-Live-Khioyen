using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 驻扎
		// 城池内部保存的驻军条目列表（可序列化保存到存档）
		// 每个条目除了指挥官信息外，还包含该指挥官在城内被分配的兵团 ID 列表。
		// 注意：序列化字段名不宜随意更改，以免影响旧存档兼容性。
		[SerializeField] List<GarrisonEntry> garrisonedEntries;

		public Action onGarrisonChanged;

		public IReadOnlyList<GameCommander> GetGarrisonedCommanders()
		{
			var list = new List<GameCommander>();
			if(garrisonedEntries != null)
			{
				foreach(var e in garrisonedEntries)
					if(e != null && e.commander != null)
						list.Add(e.commander);
			}
			return list;
		}

		public IReadOnlyList<GarrisonEntry> GetGarrisonEntries()
		{
			return garrisonedEntries;
		}

		public List<GarrisonEntry> ExtractGarrison()
		{
			// 从城池中抽出驻军数据（通常在出征/离开城池时调用），
			// 返回一个可由外部持有的副本，同时清空城池内的驻军列表。
			if(garrisonedEntries == null)
				garrisonedEntries = new List<GarrisonEntry>();
			var copy = new List<GarrisonEntry>(garrisonedEntries);
			garrisonedEntries.Clear();
			onGarrisonChanged?.Invoke();
			return copy;
		}

		public void RestoreGarrison(List<GarrisonEntry> entries)
		{
			// 恢复驻军数据到城池（通常在进入/回到城池场景时调用），
			// 注意传入的 entries 会被直接添加到城池列表中，调用方应保证 entries 的所有权和有效性。
			if(entries == null)
				return;
			if(garrisonedEntries == null)
				garrisonedEntries = new List<GarrisonEntry>();
			garrisonedEntries.AddRange(entries);
			onGarrisonChanged?.Invoke();
		}

		#endregion

		#region 提拔
		// 此值应序列化，否则按照现有的随机生成设计，不能保证每次打开存档都能稳定复现。
		[SerializeField] GameCommander nextPromotableCommander;

		public Action onNextPromotableCommanderChanged;

		/// <remarks>
		/// 在没被外部条件触发导致变化时是幂等的。
		/// </remarks>
		public GameCommander GetPromotableCommander()
		{
			// nextPromotableCommander 可能会被 Unity 的序列化设置为非 null 但全空的值
			if(nextPromotableCommander == null || string.IsNullOrEmpty(nextPromotableCommander.commanderName))
				nextPromotableCommander = CommanderRegistry.Instance.GenerateRandomCommander();

			return nextPromotableCommander;
		}

		public void PromoteCommander()
		{
			GameCommander promoted = GetPromotableCommander();

			if(promoted == null)
			{
				Debug.LogWarning("提拔失败：没有可提拔的指挥官。");
				return;
			}

			// 把当前的加到驻军列表里去
			if(garrisonedEntries == null)
				garrisonedEntries = new List<GarrisonEntry>();
			garrisonedEntries.Add(new GarrisonEntry { commander = promoted });
			Debug.Log($"提拔了武将“{promoted.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotableCommander = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion
	}

	[Serializable]
	public class GarrisonEntry
	{
		// 指挥官对象（序列化保存）
		public GameCommander commander;

		// 该指挥官在城内被分配的兵团 ID 列表（全局兵团 ID），
		// 用于在出征时将这些兵团从城池调出并在战斗场景中还原为 Batt alionStatus。
		public List<int> assignedBattalionIds = new List<int>();
	}
}
