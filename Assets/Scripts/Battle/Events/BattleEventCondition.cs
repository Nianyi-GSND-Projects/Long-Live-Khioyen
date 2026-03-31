using System;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [Serializable]
    public abstract class BattleEventCondition
    {
        // 所有子类只需要实现这一个方法即可
        public abstract bool Evaluate(BattleEventContext ctx);
    }
    
    [Serializable]
    public class ConditionGroup
    {
        [Tooltip("All conditions in this group must be TRUE (AND logic)")]
        [SerializeReference] 
        public List<BattleEventCondition> conditions = new List<BattleEventCondition>();

        public bool Evaluate(BattleEventContext ctx)
        {
            if (conditions == null || conditions.Count == 0) return true; 
        
            foreach (var condition in conditions)
            {
                if (condition == null) continue; 
                
                if (!condition.Evaluate(ctx)) return false; 
            }
            return true;
        }
    }
    
    
}