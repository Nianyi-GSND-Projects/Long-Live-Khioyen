using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Set Manager Flag")]
    public class SetManagerFlagAction : GameEventAction
    {
        public string key;
        public FlagValueType valueType; // 复用之前的枚举
        
        public bool boolValue;
        public int intValue;
        public string stringValue;

        public override void Execute()
        {
            if (BattleEventManager.Instance == null) return;

            object val = null;
            switch (valueType)
            {
                case FlagValueType.Bool: val = boolValue; break;
                case FlagValueType.Int: val = intValue; break;
                case FlagValueType.String: val = stringValue; break;
            }

            BattleEventManager.Instance.SetGlobalData(key, val);
            Debug.Log($"[Global] Set Flag '{key}' = {val}");
        }
    }
}