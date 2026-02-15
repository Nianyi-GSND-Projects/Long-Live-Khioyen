using UnityEngine;
using System;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	/// <summary>可以脱离城池活动的一支军队的容器。</summary>
	public class ArmyStatus
	{
		public GameCommander armyCommander;
		public List<BattalionStatus> battalionStatuses = new();
		public float carriedFood;
	}

	public class BattalionStatus
	{
		public int battalionId;
		//全局游戏中的部队ID，用于从全局数据中索引部队
		public string battalionName;
		public GameCommander battalionCommander;

		public BattalionDefinition battalionDefinition;
		//部队类型

		public int currentSolider;
		public int currentMorale;
		public int currentExp;
		//部队当前数据

		public int MaxSolider
		{
			get
			{
				BattleParam param = BattleParam.Instance;
				if(battalionDefinition == null)
					return 0;

				if(battalionCommander == null)
					return battalionDefinition.defaultMaxSolider;

				return (int)(battalionDefinition.defaultMaxSolider + battalionCommander.Xin * param.solidersPerXin);
			}
		}

		public int MaxMorale
		{
			get
			{
				BattleParam param = BattleParam.Instance;
				if(battalionDefinition == null)
					return 0;

				if(battalionCommander == null)
					return battalionDefinition.defaultMaxMorale;

				return (int)(battalionDefinition.defaultMaxMorale + battalionCommander.Ren * param.moralePerRen);
			}
		}

		public int MaxExp
		{
			get
			{
				BattleParam param = BattleParam.Instance;
				if(battalionDefinition == null)
					return 0;

				return param.defaultMaxExp;
			}
		}


	}

	[Serializable]
	public class GameCommander
	{
		public int commanderId;
		public string commanderName;
		public Sprite portrait;

		public int Zhi;
		public int Xin;
		public int Ren;
		public int Yong;
		public int Yan;

		public bool isAssigned;

		public List<ActionDefinition> commanderActions = new List<ActionDefinition>();
	}

}
