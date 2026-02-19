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

        private void Awake()
        {
            Instance = this;
        }

        public void OnEventTrigger(BattleEventTriggerType type)
        {
            foreach (var evt in levelEvents)
            {
                // 如果是一次性事件且已触发，跳过
                // if (triggeredEvents.Contains(evt)) continue; 

                if (evt.triggerType == type && evt.CheckConditions())
                {
                    Debug.Log($"[BattleEvent] Triggering event: {evt.eventName}");
                    evt.Trigger();
                    // triggeredEvents.Add(evt);
                }
            }
        }
    }
}