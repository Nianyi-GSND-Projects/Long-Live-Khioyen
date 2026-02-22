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

        public void StartEventExecution(BattleEventDefinition evt)
        {
            CurrentEvent = evt;
        }

        public void EndEventExecution()
        {
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

        public void OnEventTrigger(BattleEventTriggerType type)
        {
            foreach (var evt in levelEvents)
            {
                // if (triggeredEvents.Contains(evt)) continue; 

                if (evt.triggerType == type && evt.CheckConditions())
                {
                    Debug.Log($"[BattleEvent] Triggering event: {evt.eventName}");
                    StartCoroutine(evt.TriggerCoroutine());
                    // triggeredEvents.Add(evt);
                }
            }
        }
    }
}