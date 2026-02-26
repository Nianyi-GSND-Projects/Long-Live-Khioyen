using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattleEventManager : MonoBehaviour
    {
        public static BattleEventManager Instance { get; private set; }

        [Header("Config")]
        public List<BattleEventDefinition> levelEvents = new List<BattleEventDefinition>();

        // 用于防止事件重复触发的记录（可选，视需求而定）
        private HashSet<BattleEventDefinition> triggeredEvents = new HashSet<BattleEventDefinition>();
        public BattleEventDefinition CurrentEvent { get; private set; }
        public BattleEventContext CurrentContext { get; private set; }
        private Dictionary<string, object> _globalBlackboard = new Dictionary<string, object>();
        private HashSet<BattleEventDefinition> _triggeredEvents = new HashSet<BattleEventDefinition>();
        public void SetGlobalData(string key, object value)
        {
            if (_globalBlackboard.ContainsKey(key)) _globalBlackboard[key] = value;
            else _globalBlackboard.Add(key, value);
        }

        public T GetGlobalData<T>(string key)
        {
            if (_globalBlackboard.TryGetValue(key, out object val))
            {
                if (val is T tVal) return tVal;
            }
            return default(T);
        }
        
        
        
        public void StartEventExecution(BattleEventDefinition evt)
        {
            CurrentEvent = evt;
            if (evt.blockAI && Battle.Instance != null)
            {
                Battle.Instance.IsEventBlockingAI = true;
            }
        }

        public void EndEventExecution()
        {
            if (CurrentEvent != null && CurrentEvent.blockAI && Battle.Instance != null)
            {
                Battle.Instance.IsEventBlockingAI = false;
            }

            CurrentEvent = null;
        }
        
        private void Awake()
        {
            Instance = this;
            foreach(var evt in levelEvents)
            {
                if(evt != null) evt.ClearBlackboard();
            }
        }

        public void OnEventTrigger(BattleEventTriggerType type,Unit contextUnit = null)
        {
            BattleEventContext ctx = new BattleEventContext(type, contextUnit);
            CurrentContext = ctx;
            foreach (var evt in levelEvents)
            {
                if (evt == null) continue;
        
                if (evt.triggerOnce && _triggeredEvents.Contains(evt)) continue;

                if (evt.triggerType == type && evt.CheckConditions(ctx))
                {
                    if (evt.triggerOnce) _triggeredEvents.Add(evt);
                    StartCoroutine(evt.TriggerCoroutine());
                }
            }
        }
        public void ResetEvents()
        {
            _triggeredEvents.Clear();
        }
    }
}