using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
        /// <summary>
        /// 游戏的大阶段：准备、布阵、战斗、结算
        /// </summary>
        public enum Stage
        {
            Preparation,
            Arrangement,
            Battle,
            Settlement
        }
        
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
        /// <summary>
        /// 战争迷雾状态
        /// </summary>
        public enum FogState
        {
            Concealed, //完全遮蔽 (黑色)
            Explored,  // 已探索 (历史视野，灰色半透明)
            Visible    // 当前可见 (完全透明)
        }

        /// <summary>
        /// 寻找单位时的过滤器
        /// </summary>
        public enum UnitTypeFilter
        {
            All,
            BattalionOnly,
            FacilityOnly
        }

        /// <summary>
        /// 战斗中的回合流程
        /// </summary>
        public enum TurnState
        {
            PlayerTurn,
            EnemyTurn,
            FriendTurn,
            Processing
        }

        /// <summary>
        /// 玩家的操作状态机
        /// </summary>
        public enum PlayerActionStage
        {
            None,//闲置
            MovingBattalion,//选择移动位置
            SelectingAction,//选择一级行动
            SelectingSubAction,//选择次级行动
            SelectingBuildItem,//选择建造页面
            SelectingTarget,//选择目标
            SelectingAmbiguousTarget // 多重目标选择
        }

        /// <summary>
        /// 单位或地形的通行性定义
        /// </summary>
        public enum UnitPassability
        {
            Impassable,      // 阻挡一切
            Passable,        // 可通过，不可停留
            Stoppable,       // 可通过，可停留
            AlliesStoppable, // 友方可停，敌方阻挡
            AlliesPassable   // 友方可过，敌方阻挡
        }
        // 定义士兵的五种表现状态
        public enum SoldierState
        {
            Idle,       // 待机
            Move,       // 移动
            Prepare,    // 准备 
            Attack,     // 攻击
            Hit,       // 受击
            Cast        // 施法/交互
        }
        
}
