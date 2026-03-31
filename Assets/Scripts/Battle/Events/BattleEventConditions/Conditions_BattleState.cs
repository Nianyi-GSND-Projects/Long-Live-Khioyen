using System;
using UnityEngine;

namespace LongLiveKhioyen.Conditions
{
    // --- 回合 ---
    
    [Serializable]
    public class Condition_TurnCountEquals : BattleEventCondition
    {
        [Tooltip("触发条件所需的回合数")]
        public int targetTurn;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;
            return Battle.Instance.TurnCount == targetTurn;
        }
    }

    [Serializable]
    public class Condition_TurnCountGreaterThan : BattleEventCondition
    {
        [Tooltip("回合数必须大于此值")]
        public int targetTurn;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;
            return Battle.Instance.TurnCount > targetTurn;
        }
    }

    // --- 全局 Flag ---

    [Serializable]
    public class Condition_GlobalFlagIsTrue : BattleEventCondition
    {
        [Tooltip("要检查的全局黑板 Key")]
        public string flagKey;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (BattleEventManager.Instance != null)
            {
                return BattleEventManager.Instance.GetGlobalData<bool>(flagKey);
            }
            return false;
        }
    }
}