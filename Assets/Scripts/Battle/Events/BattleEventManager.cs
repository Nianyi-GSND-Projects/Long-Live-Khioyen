using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattleEventManager : MonoBehaviour
    {
        public static BattleEventManager Instance { get; private set; }

        [Header("Config")]
        public List<BattleEventDefinition> levelEvents = new List<BattleEventDefinition>();
        public BattleEventDefinition CurrentEvent { get; private set; }
        public BattleEventContext CurrentContext { get; private set; }
        private Dictionary<string, object> _globalBlackboard = new Dictionary<string, object>();
        private HashSet<BattleEventDefinition> _triggeredEvents = new HashSet<BattleEventDefinition>();
        
        private Queue<QueuedEvent> _eventQueue = new Queue<QueuedEvent>();
        private bool _isProcessingQueue = false;
        
        private struct QueuedEvent
        {
            public BattleEventDefinition EventDef;
            public BattleEventContext Context;
        }
        
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

        private void Awake()
        {
            Instance = this;
        }

        
        public void OnEventTrigger(BattleEventTriggerType type, Unit contextUnit = null)
        {
            BattleEventContext ctx = new BattleEventContext(type, contextUnit);

            foreach (var evt in levelEvents)
            {
                if (evt == null || (evt.triggerOnce && _triggeredEvents.Contains(evt))) continue;

                if (evt.triggerType == type)
                {
                    _eventQueue.Enqueue(new QueuedEvent { EventDef = evt, Context = ctx });
                }
            }

            if (!_isProcessingQueue && _eventQueue.Count > 0)
            {
                StartCoroutine(ProcessEventQueueCoroutine());
            }
            
        }
        
        private IEnumerator ProcessEventQueueCoroutine()
        {
            _isProcessingQueue = true;

            while (_eventQueue.Count > 0)
            {
                var queuedItem = _eventQueue.Dequeue();
                var evt = queuedItem.EventDef;
                
                if (evt.triggerOnce && _triggeredEvents.Contains(evt)) continue;

                if (!evt.CheckConditions(queuedItem.Context))
                {
                    // 条件不满足，直接跳过，看队列里的下一个
                    continue; 
                }
                
                if (evt.triggerOnce) _triggeredEvents.Add(evt);
                
                // 每次执行前，精准恢复该事件的 Context 和 CurrentEvent
                CurrentEvent = queuedItem.EventDef;
                CurrentContext = queuedItem.Context; 

                if (CurrentEvent.blockAI && Battle.Instance != null)
                {
                    Battle.Instance.IsEventBlockingAI = true;
                }

                // 等待事件执行
                yield return StartCoroutine(CurrentEvent.TriggerCoroutine(queuedItem.Context));

                // 使用局部变量判断，防止内部逻辑意外置空 CurrentEvent 导致报错
                if (CurrentEvent != null && CurrentEvent.blockAI && Battle.Instance != null)
                {
                    Battle.Instance.IsEventBlockingAI = false;
                }
        
                CurrentEvent = null;
            }

            _isProcessingQueue = false;
            CurrentContext = null;
        }
        
        public void ResetEvents()
        {
            _triggeredEvents.Clear();
        }
    }
}