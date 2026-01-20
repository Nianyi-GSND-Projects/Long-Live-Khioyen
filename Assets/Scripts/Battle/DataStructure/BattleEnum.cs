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
            None,
            MovingBattalion,
            SelectingAction,
            SelectingTarget,
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
    
}
