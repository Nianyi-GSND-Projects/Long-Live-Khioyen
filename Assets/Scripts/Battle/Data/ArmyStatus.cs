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
                if (instance == null) instance = new ArmyStatus();
                return instance;
            }
        }
        #endregion
        
        public ArmyCommander armyCommander;
        public List<BattalionStatus> battalionStatuses = new List<BattalionStatus>();
    }
    
    public class BattalionStatus
    {
        public int battalionId;
        //全局游戏中的部队ID，用于从全局数据中索引部队
        public string battalionName;
        public BattalionCommander battalionCommander;
        
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

    public class ArmyCommander
    {
        public int commanderId;
        public string commanderName;
        
        public int Zhi;
        public int Xin;
        public int Ren;
        public int Yong;
        public int Yan;
        
    }

    public class BattalionCommander
    {
        public int commanderId;
        public string commanderName;
        
        public int Zhi;//智 提高谋略效果，提高对谋略与战法的抵抗
        public int Xin;//信 提高士兵上限，提高从战斗中获得的经验值
        public int Ren;//仁 提高伤兵恢复，减少士气与补给消耗
        public int Yong;//勇 提高部队攻击力，提高战法效果
        public int Yan;//严 提高从训练中获得的经验值，提高设施建设速度
        
    }
    
    
}
