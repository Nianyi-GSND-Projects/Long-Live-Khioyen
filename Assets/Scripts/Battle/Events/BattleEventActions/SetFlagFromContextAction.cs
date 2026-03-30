using UnityEngine;

namespace LongLiveKhioyen
{
    public enum ContextSourceType
    {
        UnitName,
        UnitID,
        UnitFaction
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Set Flag From Context")]
    public class SetFlagFromContextAction : GameEventAction
    {
        [Header("Settings")]
        public string outputKey;
        public ContextSourceType sourceType;

        // ==========================================
        // 1. 无参版本 (警告：缺少上下文无法提取数据)
        // ==========================================
        public override void Execute()
        {
            Debug.LogWarning($"[SetFlagFromContextAction] 执行失败：缺少 BattleEventContext。无法提取触发单位的数据并设置 '{outputKey}'。");
        }

        // ==========================================
        // 2. 战斗专用版本 (读写合一的 Context)
        // ==========================================
        public override void Execute(BattleEventContext ctx)
        {
            if (ctx == null)
            {
                Execute();
                return;
            }

            if (ctx.TriggerUnit == null)
            {
                Debug.LogWarning($"[SetFlagFromContextAction] 触发单位 (TriggerUnit) 为空，无法提取 {sourceType} 到 '{outputKey}'。");
                return;
            }

            object val = null;
            switch (sourceType)
            {
                case ContextSourceType.UnitName:
                    // 假设你需要的是 GameObject 的名字，或者如果你有 unitDefinition 可以改成 ctx.TriggerUnit.unitDefinition.unitName
                    val = ctx.TriggerUnit.name; 
                    break;
                case ContextSourceType.UnitID:
                    val = ctx.TriggerUnit.InstanceId;
                    break;
                case ContextSourceType.UnitFaction:
                    val = ctx.TriggerUnit.faction; // 假设你的 Unit 类中有 faction 字段
                    break;
            }
            
            // 直接将提取到的数据写回同一个 Context 中
            ctx.SetData(outputKey, val);
            Debug.Log($"[SetFlagFromContext] 成功将 {sourceType} ({val}) 提取并存入 '{outputKey}' 中。");
        }
    }
}