using System.Collections;
using UnityEngine;


namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Knockback")]
    public class KnockbackEffect : EffectDefinition
    {
        [Header("Settings")]
        public int pushDistance = 1;
        
        [Tooltip("如果勾选，遇到阻挡直接停在阻挡前；如果不勾选，遇到阻挡则完全不移动")]
        public bool pushAsFarAsPossible = false;

        [Header("Output")]
        [Tooltip("将结果写入上下文的Key，供后续Effect读取")]
        public string resultKey = "IsKnockbackBlocked";
        public string collisionTargetKey = "CollisionTarget";

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            if (target == null || !target.unitDefinition.beMoved) // 假设设施不可被击退
            {
                ctx.SetData(resultKey, true); // 视为被阻挡
                yield break;
            }
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;

            Battalion attackerBat = ctx.User as Battalion;
            Battalion defenderBat = target as Battalion;

            if (camController != null && ctx.User != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(ctx.User.position), focusDist, camTransitionTime);
            }
            if (attackerBat != null) attackerBat.CurrentSoldierState = SoldierState.Attack;
            if (defenderBat != null) defenderBat.CurrentSoldierState = SoldierState.Hit;
            
            // 等待 t 秒，营造打击停顿感
            yield return new WaitForSeconds(t);

            // ==========================================
            // 逻辑执行阶段：处理网格位移
            // ==========================================
            Vector2Int currentPos = target.position;
            Vector3Int pushDir = Battle.Instance.GetHexDirection(ctx.User.position, currentPos);
            
            bool blocked = false;
            Unit collisionObject = null; // 记录撞到了谁

            for (int i = 1; i <= pushDistance; i++)
            {
                Vector2Int nextPos = Battle.Instance.GetTileInDirection(currentPos, pushDir, 1);
                
                if (Battle.Instance.IsValidMapPosition(nextPos) &&
                    Battle.Instance.CanUnitStopOnTile(target, nextPos, false))
                {
                    // 移动一步
                    yield return Battle.Instance.ForceMoveUnitRoutine(target, nextPos);
                    currentPos = nextPos;
                }
                else
                {
                    if (Battle.Instance.IsValidMapPosition(nextPos))
                    {
                        var tile = Battle.Instance.mapData[nextPos.x, nextPos.y];
                        collisionObject = tile.Battalion != null ? (Unit)tile.Battalion : tile.Facility;
                    }

                    blocked = true;
                    break;
                }
            }
            
            ctx.SetData(resultKey, blocked);
            
            if (blocked && collisionObject != null)
            {
                ctx.SetData(collisionTargetKey, collisionObject);
            }
            
            // ==========================================
            // 动画阶段 2：追踪目标位移后的新位置
            // ==========================================
            if (camController != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(target.position), focusDist, camTransitionTime);
            }

            // 再等待 t 秒，让镜头跟过去，并让玩家看清击退落地后的样子
            yield return new WaitForSeconds(t);

        }
    }
}