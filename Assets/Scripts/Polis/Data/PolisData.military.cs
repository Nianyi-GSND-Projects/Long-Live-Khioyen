using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		#region 驻扎
		[SerializeField] List<GameCommander> garrisonedCommanders;

		public Action onGarrisonChanged;

		public IReadOnlyList<GameCommander> GetGarrisonedCommanders()
		{
			return garrisonedCommanders;
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
			garrisonedCommanders.Add(promoted);
			Debug.Log($"提拔了武将“{promoted.commanderName}”");
			onGarrisonChanged?.Invoke();

			// 刷新下一个
			nextPromotableCommander = null;
			onNextPromotableCommanderChanged?.Invoke();
		}
		#endregion
	}
}
