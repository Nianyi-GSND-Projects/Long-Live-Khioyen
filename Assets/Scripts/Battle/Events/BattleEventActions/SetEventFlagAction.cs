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
        public Vector2Int valueA_Vector; 

        [Tooltip("Value if condition is FALSE")]
        public string valueB_String;
        public int valueB_Int;
        public bool valueB_Bool;
        public Vector2Int valueB_Vector;

        // ==========================================
        // 1. 无参版本 (警告：缺少上下文无法读写 Flag)
        // ==========================================
        public override void Execute()
        {
            Debug.LogWarning($"[SetEventFlagAction] 执行失败：缺少 BattleEventContext。无法读取条件或设置 '{outputKey}'。");
        }

        // ==========================================
        // 2. 战斗专用版本 (读写 Context 数据)
        // ==========================================
        public override void Execute(BattleEventContext ctx)
        {
            if (ctx == null)
            {
                Execute();
                return;
            }

            // 将条件检查和数值写入全部切换到传入的 ctx 上
            bool conditionMet = CheckCondition(ctx);
            object finalValue = GetValue(conditionMet);
            
            ctx.SetData(outputKey, finalValue);
            Debug.Log($"[SetFlag] Condition: {conditionMode} -> {conditionMet}. Set '{outputKey}' = {finalValue} in Context");
        }

        // [修改] 参数从 BattleEventDefinition 改为 BattleEventContext
        private bool CheckCondition(BattleEventContext ctx)
        {
            switch (conditionMode)
            {
                case FlagConditionMode.Always:
                    return true;

                case FlagConditionMode.KeyExists:
                    return ctx.HasData(checkKey);

                case FlagConditionMode.KeyNotExists:
                    return !ctx.HasData(checkKey);

                case FlagConditionMode.Equals:
                    if (!ctx.HasData(checkKey)) return false;
                    return IsEqual(ctx.GetData<object>(checkKey));

                case FlagConditionMode.NotEquals:
                    if (!ctx.HasData(checkKey)) return true; // Key 不存在视为不相等
                    return !IsEqual(ctx.GetData<object>(checkKey));

                case FlagConditionMode.GreaterThan:
                    if (!ctx.HasData(checkKey)) return false;
                    return GetInt(ctx.GetData<object>(checkKey)) > checkValueInt;

                case FlagConditionMode.LessThan:
                    if (!ctx.HasData(checkKey)) return false;
                    return GetInt(ctx.GetData<object>(checkKey)) < checkValueInt;
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