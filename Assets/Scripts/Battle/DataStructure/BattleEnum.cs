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
        
}
