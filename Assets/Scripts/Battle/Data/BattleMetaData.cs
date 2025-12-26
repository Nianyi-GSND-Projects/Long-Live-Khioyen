using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public enum BattleType
    {
        Seige, //攻城战
        Defend, //守城战
        Encounter //遭遇战
    }

    public enum BattleGoal
    {
        Annihilate, //歼灭战：所有敌人离开战场即获胜
        Guard,//防守战：规定回合数内敌人无法占领目标点或我方依然有部队存活即获胜
        Convey,//护送目标单位撤离抵达目标点
        Escape //有部队撤离战斗即可获胜
        
    }

    public class BattleMetaData
    {
        public string battleName;
        //战役名称
        public int battleId;
        //战役id
        public int battleTime;
        //战役发生时间
        public BattleType battleType;
        //战役类型

        public Vector2 battlePosition;
        //战役发生地在大地图上的坐标
        public Vector2 encounterOrientation;
        //玩家进入战役时行军的方向向量
        
        public Vector2Int battleSize;
        //战斗场地规模
        
        public BattleGoal battleGoal;

        public int enemyCount;
        //TODO MetaData中应该包含一系列更具体指导地图生成的数据，例如：如果有预设地图，那预设地图的索引是多少；如果没有，则根据一系列参数随机生成地图。
    }
    
}
