using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Retreat")]
    public class RetreatEffect : EffectDefinition
    {
        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit unit = ctx.User;
            if (unit == null) yield break;
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;

            // ==========================================
            // 动画阶段：切换为移动状态，表现“撤退跑路”
            // ==========================================
            if (camController != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(unit.position), focusDist, camTransitionTime);
            }
            Battalion retreatBat = unit as Battalion;
            if (retreatBat != null)
            {
                retreatBat.CurrentSoldierState = SoldierState.Move;
            }

            // 等待 t 秒，让撤退动作展示一会儿
            yield return new WaitForSeconds(t);

            // ==========================================
            // 逻辑执行阶段：正式撤离
            // ==========================================
            Debug.Log($"{unit.name} 已成功撤离战场！");

            // 1. 从地图移除
            if (Battle.Instance != null)
            {
                Battle.Instance.RemoveUnitFromMap(unit);
            
                // 2. 从活跃列表移除 (这会影响胜利条件判断)
                Battle.Instance.WithdrawUnit(unit); 
            }

            unit.gameObject.SetActive(false);
        }
    }
}