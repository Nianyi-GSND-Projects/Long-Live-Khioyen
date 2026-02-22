using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattleEventContext
    {
        public BattleEventTriggerType TriggerType;
        public Unit TriggerUnit;
        
        // 预留扩展
        public UnityEngine.Vector2Int? TriggerPos;
        public int? TriggerValue;

        public BattleEventContext(BattleEventTriggerType type, Unit unit = null)
        {
            TriggerType = type;
            TriggerUnit = unit;
        }
    }
}
