using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Interact With Facility")]
    public class InteractEffect : EffectDefinition
    {
        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            Unit user = ctx.User;
            if (target is Facility facility)
            {
                float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
                float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
                float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

                BattleCameraController camController = null;
                if (Battle.Instance.inputController != null)
                    camController = Battle.Instance.inputController.cameraController;
                
                // 检查是否可交互
                if (facility.Definition.isInteractable)
                {
                    Battalion interactorBat = user as Battalion;

                    // ==========================================
                    // 动画阶段 1：聚焦交互者，播放 Cast 动作
                    // ==========================================
                    if (camController != null && user != null)
                    {
                        camController.FocusOnPosition(Battle.Instance.MapToWorld(user.position), focusDist, camTransitionTime);
                    }
                    if (interactorBat != null) interactorBat.CurrentSoldierState = SoldierState.Cast; 
                
                    yield return new WaitForSeconds(t);

                    // ==========================================
                    // 动画阶段 2：聚焦设施，执行交互逻辑
                    // ==========================================
                    if (camController != null)
                    {
                        camController.FocusOnPosition(Battle.Instance.MapToWorld(facility.position), focusDist, camTransitionTime);
                    }

                    facility.Definition.OnInteract(user, facility);
                    Debug.Log($"[Effect] {user.name} 成功触发了 {facility.name} 的交互逻辑。");

                    yield return new WaitForSeconds(t);
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

            yield break;
        }
    }
}