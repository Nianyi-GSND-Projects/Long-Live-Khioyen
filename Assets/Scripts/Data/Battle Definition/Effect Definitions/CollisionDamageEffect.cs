using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Collision Damage")]
    public class CollisionDamageEffect : EffectDefinition
    {
        [Header("Settings")]
        public string targetKey = "CollisionTarget";
        
        [Header("Input Damage")]
        public bool useDamageFromContext = true;
        public string inputDamageKey = "LastDamageAmount";
        public float fallbackDamage = 0; // 如果没读到伤害，用这个

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit victim = ctx.GetData<Unit>(targetKey);
            if (victim == null) yield break;

            int damageToApply = (int)fallbackDamage;
            
            if (useDamageFromContext)
            {
                object val = ctx.GetData<object>(inputDamageKey);
                if (val != null)
                {
                    damageToApply = System.Convert.ToInt32(val);
                }
            }
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            // 获取相机控制器
            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;

            // ==========================================
            // 动画阶段：目标进入受击状态
            // ==========================================
            Battalion victimBat = victim as Battalion;
            if (victimBat != null)
            {
                victimBat.CurrentSoldierState = SoldierState.Hit;
            }
            if (camController != null)
            {
                // 将镜头甩向被撞击者
                camController.FocusOnPosition(Battle.Instance.MapToWorld(victim.position), focusDist, camTransitionTime);
            }

            // 等待 t 秒，让受击动作充分展示
            yield return new WaitForSeconds(t);

            // ==========================================
            // 逻辑执行阶段：造成伤害
            // ==========================================
            Debug.Log($"{victim.name} 受到连带伤害: {damageToApply}");
            victim.TakeDamage(damageToApply);
        }
    }
}