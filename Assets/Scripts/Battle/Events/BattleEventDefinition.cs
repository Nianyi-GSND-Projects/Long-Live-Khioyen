using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    public enum BattleEventTriggerType
    {
        OnBattleStart,
        OnTurnStart,
        OnTurnEnd,
        OnUnitDeath,
        OnPlayerActionEnd,
        Manual
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Battle Event Definition")]
    public class BattleEventDefinition : ScriptableObject
    {
        [Header("id")] public int id;
        public string eventName;
        public BattleEventTriggerType triggerType;
        
        [Header("Conditions")]
        public List<BattleEventCondition> conditions = new List<BattleEventCondition>();
        //用于保存“整个BattleEventDefinition被触发”的前提条件
        [Header("Actions")]
        public List<GameEventAction> actions = new List<GameEventAction>();
        
        [Header("Context")]
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();
        //用来保存本BattleEventDefinition内的每个GameEventAction执行相关的条件信息
        
        public bool CheckConditions()
        {
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate()) return false;
            }
            return true;
        }

        public void Trigger()
        {
            if (BattleEventManager.Instance != null)
            {
                BattleEventManager.Instance.StartEventExecution(this);
            }

            foreach (var action in actions)
            {
                if (action != null) action.Execute();
            }
            
            if (BattleEventManager.Instance != null)
            {
                BattleEventManager.Instance.EndEventExecution();
            }
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