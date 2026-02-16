using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/General/Interact With Facility")]
    public class InteractEffect : EffectDefinition
    {
        public override void Execute(ActionContext ctx)
        {
            
            Unit target = ctx.TargetUnit;

            if (target is Facility facility)
            {
                // 检查是否可交互 (双重保险，虽然 Action Condition 可能已经查过了)
                if (facility.Definition.isInteractable)
                {
                    // 执行具体的多态交互逻辑
                    facility.Definition.OnInteract(ctx.User, facility);
                    Debug.Log($"[Effect] {ctx.User.name} 成功触发了 {facility.name} 的交互逻辑。");
                }
                else
                {
                    Debug.LogWarning($"[Effect] 目标设施 {facility.name} 不可交互。");
                }
            }
            else
            {
                // 如果目标不是设施 (比如点到了空地，或者逻辑错误点到了人)
                Debug.LogWarning($"[Effect] 交互失败：目标不是设施 (Target: {target?.name ?? "Null"})");
            }
        }
    }
}