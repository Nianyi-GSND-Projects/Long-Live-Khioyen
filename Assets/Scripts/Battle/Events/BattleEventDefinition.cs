using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

namespace LongLiveKhioyen
{
    public enum BattleEventTriggerType
    {
        OnBattleStart,
        OnPlayerTurnStart,
        OnPlayerTurnEnd,
        OnEnemyTurnStart,
        OnEnemyTurnEnd,
        OnUnitDeath,
        OnUnitActionEnd,
        OnTileEnter,
        Manual
    }
    
    

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Battle Event Definition")]
    public class BattleEventDefinition : ScriptableObject
    {
        [Header("id")] public int id;
        public string eventName;
        public BattleEventTriggerType triggerType;
        
        [Header("Settings")]
        public bool triggerOnce = true;
        public bool blockAI = true; // [新增]
        
        [Header("Trigger Logic")]
        [Tooltip("Any group evaluating to TRUE will trigger the event (OR logic between groups)")]
        public List<ConditionGroup> conditionGroups = new List<ConditionGroup>();

        //用于保存“整个BattleEventDefinition被触发”的前提条件
        [Header("Actions")]
        public List<GameEventAction> actions = new List<GameEventAction>();
        
        public bool CheckConditions(BattleEventContext ctx)
        {
            if (conditionGroups == null || conditionGroups.Count == 0) return true;

            foreach (var group in conditionGroups)
            {
                if (group == null) continue; 
        
                if (group.Evaluate(ctx)) return true;
            }
    
            return false; 
        }
        
        public IEnumerator TriggerCoroutine(BattleEventContext ctx)
        {
            foreach (var action in actions)
            {
                if (action == null) continue;

                if (action.isBlocking)
                {
                    yield return action.ExecuteCoroutine(ctx); 
                }
                else
                {
                    MonoBehaviour runner = BattleEventManager.Instance;
                    if (ctx != null && ctx.TriggerUnit != null && ctx.TriggerUnit.gameObject.activeInHierarchy)
                    {
                        runner = ctx.TriggerUnit; 
                    }
                    
                    if (runner != null && runner.gameObject.activeInHierarchy)
                    {
                        runner.StartCoroutine(action.ExecuteCoroutine(ctx));
                    }
                    else
                    {
                        action.Execute(ctx);
                    }
                }
            }
        }
    }
}