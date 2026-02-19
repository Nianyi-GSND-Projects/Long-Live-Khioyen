using System.Collections.Generic;
using UnityEngine;

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
        public string eventName;
        public BattleEventTriggerType triggerType;
        
        [Header("Conditions")]
        public List<BattleEventCondition> conditions = new List<BattleEventCondition>();

        [Header("Actions")]
        public List<GameEventAction> actions = new List<GameEventAction>();

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
            foreach (var action in actions)
            {
                if (action != null) action.Execute();
            }
        }
    }
}