using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    
    public class ArmyStatus
    {
        #region Singleton
        
        static ArmyStatus instance;
        public static ArmyStatus Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArmyStatus();
                    var ensureRegistryInit = CommanderRegistry.Instance; 
                }
                return instance;
            }
        }
        #endregion
        
        public GameCommander armyCommander;
        public List<BattalionStatus> battalionStatuses = new List<BattalionStatus>();
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

                if (battalionCommander == null)
                    return battalionDefinition.defaultMaxSolider;
                
                return (int)(battalionDefinition.defaultMaxSolider + battalionCommander.Xin * param.solidersPerXin);
            }
        }

        public int MaxMorale
        {
            get
            {
                BattleParam param = BattleParam.Instance;
                if (battalionDefinition == null)
                    return 0;

                if (battalionCommander == null)
                    return battalionDefinition.defaultMaxMorale;

                return (int)(battalionDefinition.defaultMaxMorale + battalionCommander.Ren * param.moralePerRen);
            }
        }

        public int MaxExp
        {
            get
            {
                BattleParam param = BattleParam.Instance;
                if (battalionDefinition == null)
                    return 0;

                return param.defaultMaxExp;
            }
        }
        
        
    }

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
    }

}
