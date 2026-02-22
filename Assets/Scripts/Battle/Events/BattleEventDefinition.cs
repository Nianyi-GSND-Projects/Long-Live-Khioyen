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
        Manual
    }
    
    [Serializable]
    public class ConditionGroup
    {
        [Tooltip("All conditions in this group must be TRUE (AND logic)")]
        public List<BattleEventCondition> conditions = new List<BattleEventCondition>();

        public bool Evaluate(BattleEventContext ctx)
        {
            if (conditions.Count == 0) return true; // 空组默认为真

            foreach (var condition in conditions)
            {
                if (!condition.Evaluate(ctx)) return false;
            }
            return true;
        }
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Battle Event Definition")]
    public class BattleEventDefinition : ScriptableObject
    {
        [Header("id")] public int id;
        public string eventName;
        public BattleEventTriggerType triggerType;
        
        [Header("Trigger Logic")]
        [Tooltip("Any group evaluating to TRUE will trigger the event (OR logic between groups)")]
        public List<ConditionGroup> conditionGroups = new List<ConditionGroup>();

        //用于保存“整个BattleEventDefinition被触发”的前提条件
        [Header("Actions")]
        public List<GameEventAction> actions = new List<GameEventAction>();
        
        [Header("Context")]
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();
        //用来保存本BattleEventDefinition内的每个GameEventAction执行相关的条件信息
        
        public bool CheckConditions(BattleEventContext ctx)
        {
            // 如果没有配置任何条件，默认为真 (无条件触发)
            if (conditionGroups.Count == 0) return true;

            foreach (var group in conditionGroups)
            {
                // 只要有一个组满足 (OR)
                if (group.Evaluate(ctx)) return true;
            }
            
            return false; // 所有组都不满足
        }
        
        public IEnumerator TriggerCoroutine()
        {
            if (BattleEventManager.Instance != null)
                BattleEventManager.Instance.StartEventExecution(this);

            foreach (var action in actions)
            {
                if (action == null) continue;

                if (action.isBlocking)
                {
                    // 阻塞执行
                    yield return  action.ExecuteCoroutine();
                }
                else
                {
                    // 非阻塞执行 (直接跑，不等待)
                    if (BattleEventManager.Instance != null)
                        BattleEventManager.Instance.StartCoroutine(action.ExecuteCoroutine());
                    else
                        action.Execute(); // Fallback
                }
            }
            
            if (BattleEventManager.Instance != null)
                BattleEventManager.Instance.EndEventExecution();
        }

        #region blackboard

        public void SetData(string key, object value)
        {
            if (_blackboard.ContainsKey(key)) _blackboard[key] = value;
            else _blackboard.Add(key, value);
        }

        public T GetData<T>(string key)
        {
            if (_blackboard.TryGetValue(key, out object val))
            {
                if (val is T tVal) return tVal;
            }
            return default(T);
        }
        
        public bool HasData(string key) => _blackboard.ContainsKey(key);
        
        public void ClearBlackboard()
        {
            _blackboard.Clear();
        }

        #endregion
        

    }
}