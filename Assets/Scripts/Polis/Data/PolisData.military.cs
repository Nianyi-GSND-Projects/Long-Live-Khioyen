using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 驻扎
		// 城池内部保存的驻军列表（可序列化保存到存档）
		// 每个驻军为一个完整的 ArmyStatus，包含指挥官与其兵团状态
		// 注意：序列化字段名不宜随意更改，以免影响旧存档兼容性。
		[SerializeField] List<ArmyStatus> garrisonedArmies;

		public Action onGarrisonChanged;

		public IReadOnlyList<GameCommander> GetGarrisonedCommanders()
		{
			var list = new List<GameCommander>();
			if(garrisonedArmies != null)
			{
				foreach(var army in garrisonedArmies)
					if(army != null && army.armyCommander != null)
						list.Add(army.armyCommander);
			}
			return list;
		}

		public IReadOnlyList<ArmyStatus> GetGarrisonedArmies()
		{
			return garrisonedArmies;
		}

		public List<ArmyStatus> ExtractGarrison()
		{
			// 从城池中抽出驻军数据（通常在出征/离开城池时调用），
			// 返回一个可由外部持有的副本，同时清空城池内的驻军列表。
			if(garrisonedArmies == null)
				garrisonedArmies = new List<ArmyStatus>();
			var copy = new List<ArmyStatus>(garrisonedArmies);
			garrisonedArmies.Clear();
			onGarrisonChanged?.Invoke();
			return copy;
		}

		public void RestoreGarrison(List<ArmyStatus> armies)
		{
			// 恢复驻军数据到城池（通常在进入/回到城池场景时调用），
			// 注意传入的 armies 会被直接添加到城池列表中，调用方应保证引用的有效性。
			if(armies == null)
				return;
			if(garrisonedArmies == null)
				garrisonedArmies = new List<ArmyStatus>();
			garrisonedArmies.AddRange(armies);
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
			if(garrisonedArmies == null)
				garrisonedArmies = new List<ArmyStatus>();
			var newArmy = new ArmyStatus();
			newArmy.armyCommander = promoted;
			garrisonedArmies.Add(newArmy);
			Debug.Log($"提拔了武将“{promoted.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotableCommander = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion
	}


}
