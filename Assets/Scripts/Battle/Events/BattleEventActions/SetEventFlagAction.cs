using UnityEngine;

namespace LongLiveKhioyen
{
    public enum FlagValueType
    {
        Bool,
        Int,
        String,
        Vector2Int
    }

    public enum FlagConditionMode
    {
        Always,
        KeyExists,
        KeyNotExists,
        Equals,
        NotEquals,
        GreaterThan, // 仅限数字
        LessThan     // 仅限数字
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Set Event Flag (Conditional)")]
    public class SetEventFlagAction : GameEventAction
    {
        [Header("Output")]
        public string outputKey;
        public FlagValueType valueType; // 输出值的类型

        [Header("Condition")]
        public FlagConditionMode conditionMode = FlagConditionMode.Always;
        public string checkKey; // 要检查的 Key
        
        // 检查用的值 (目前简化为 int，如果需要更复杂类型可扩展)
        public int checkValueInt;
        public string checkValueString;

        [Header("Values")]
        [Tooltip("Value if condition is TRUE")]
        public string valueA_String;
        public int valueA_Int;
        public bool valueA_Bool;
        public Vector2Int valueA_Vector; // [新增]

        [Tooltip("Value if condition is FALSE")]
        public string valueB_String;
        public int valueB_Int;
        public bool valueB_Bool;
        public Vector2Int valueB_Vector;

        public override void Execute()
        {
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentEvent == null) return;
            var evt = BattleEventManager.Instance.CurrentEvent;

            bool conditionMet = CheckCondition(evt);
            
            object finalValue = GetValue(conditionMet);
            
            evt.SetData(outputKey, finalValue);
            Debug.Log($"[SetFlag] Condition: {conditionMode} -> {conditionMet}. Set '{outputKey}' = {finalValue}");
        }

        private bool CheckCondition(BattleEventDefinition evt)
        {
            switch (conditionMode)
            {
                case FlagConditionMode.Always:
                    return true;

                case FlagConditionMode.KeyExists:
                    return evt.HasData(checkKey);

                case FlagConditionMode.KeyNotExists:
                    return !evt.HasData(checkKey);

                case FlagConditionMode.Equals:
                    if (!evt.HasData(checkKey)) return false;
                    return IsEqual(evt.GetData<object>(checkKey));

                case FlagConditionMode.NotEquals:
                    if (!evt.HasData(checkKey)) return true; // Key 不存在视为不相等
                    return !IsEqual(evt.GetData<object>(checkKey));

                case FlagConditionMode.GreaterThan:
                    if (!evt.HasData(checkKey)) return false;
                    return GetInt(evt.GetData<object>(checkKey)) > checkValueInt;

                case FlagConditionMode.LessThan:
                    if (!evt.HasData(checkKey)) return false;
                    return GetInt(evt.GetData<object>(checkKey)) < checkValueInt;
            }
            return false;
        }

        private bool IsEqual(object val)
        {
            if (val is int iVal) return iVal == checkValueInt;
            if (val is string sVal) return sVal == checkValueString;
            return false;
        }

        private int GetInt(object val)
        {
            if (val is int i) return i;
            if (val is float f) return (int)f;
            return 0;
        }

        private object GetValue(bool isA)
        {
            switch (valueType)
            {
                case FlagValueType.Bool: return isA ? valueA_Bool : valueB_Bool;
                case FlagValueType.Int: return isA ? valueA_Int : valueB_Int;
                case FlagValueType.String: return isA ? valueA_String : valueB_String;
                case FlagValueType.Vector2Int: return isA ? valueA_Vector : valueB_Vector;
            }
            return null;
        }
    }
}