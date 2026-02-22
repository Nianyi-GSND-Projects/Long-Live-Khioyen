using UnityEngine;

namespace LongLiveKhioyen
{
    public enum FlagValueType
    {
        Bool,
        Int,
        String
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Set Event Flag")]
    public class SetEventFlagAction : GameEventAction
    {
        public string key;
        public FlagValueType valueType;
        
        public bool boolValue;
        public int intValue;
        public string stringValue;

        public override void Execute()
        {
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentEvent == null)
            {
                Debug.LogWarning("SetEventFlagAction: No active event context found.");
                return;
            }

            object val = null;
            switch (valueType)
            {
                case FlagValueType.Bool: val = boolValue; break;
                case FlagValueType.Int: val = intValue; break;
                case FlagValueType.String: val = stringValue; break;
            }

            BattleEventManager.Instance.CurrentEvent.SetData(key, val);
            Debug.Log($"[Event] Set Flag '{key}' = {val}");
        }
    }
}