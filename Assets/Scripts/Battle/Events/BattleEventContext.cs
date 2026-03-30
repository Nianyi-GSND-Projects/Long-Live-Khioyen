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
        
        private Dictionary<string, object> _localBlackboard = new Dictionary<string, object>();
        
        public BattleEventContext(BattleEventTriggerType type, Unit unit = null)
        {
            TriggerType = type;
            TriggerUnit = unit;
        }
        
        #region Local Blackboard
        
        public void SetData(string key, object value)
        {
            if (_localBlackboard.ContainsKey(key)) _localBlackboard[key] = value;
            else _localBlackboard.Add(key, value);
        }

        public T GetData<T>(string key)
        {
            if (_localBlackboard.TryGetValue(key, out object val) && val is T tVal)
            {
                return tVal;
            }
            return default;
        }
        
        public bool HasData(string key) => _localBlackboard.ContainsKey(key);
        public void ClearBlackboard() => _localBlackboard.Clear();

        #endregion
    }
}
