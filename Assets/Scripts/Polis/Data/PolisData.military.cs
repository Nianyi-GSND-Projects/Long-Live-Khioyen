using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 驻扎
		[SerializeField] List<BattalionStatus> garrisonedBattalions = new();

		public Action onGarrisonChanged;

		public IReadOnlyList<GameCommander> GetGarrisonedCommanders()
		{
			return garrisonedBattalions.Select(b => b.battalionCommander).ToList();
		}

		/// <summary>使军队出城。</summary>
		public ArmyStatus LetOutGarrison(GameCommander headCommander, float foodAmount, IReadOnlyList<GameCommander> battalionCommanders)
		{
			var battalions = garrisonedBattalions.Where(b => battalionCommanders.Contains(b.battalionCommander)).ToList();
			foreach(var b in battalions)
				garrisonedBattalions.Remove(b);
			ArmyStatus army = new()
			{
				armyCommander = headCommander,
				battalionStatuses = battalions,
				carriedFood = foodAmount,
			};
			return army;
		}

		/// <summary>使军队驻扎到城内。</summary>
		public void GarrisonArmy(ArmyStatus army)
		{
			garrisonedBattalions.AddRange(army.battalionStatuses);
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
			GameCommander promotedCommander = GetPromotableCommander();

			if(promotedCommander == null)
			{
				Debug.LogWarning("提拔失败：没有可提拔的指挥官。");
				return;
			}

			// 把当前的加到驻军列表里去
			BattalionStatus battalion = new()
			{
				battalionCommander = promotedCommander,
			};
			garrisonedBattalions.Add(battalion);

			Debug.Log($"提拔了武将“{promotedCommander.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotableCommander = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion
	}


}
