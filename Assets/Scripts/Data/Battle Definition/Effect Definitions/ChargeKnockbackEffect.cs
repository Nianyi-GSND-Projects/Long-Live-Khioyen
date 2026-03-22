using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Charge Knockback (Push & Follow)")]
    public class ChargeKnockbackEffect : EffectDefinition
    {
        [Header("Settings")]
        public int pushDistance = 1;
        
        [Header("Output")]
        public string resultKey = "IsKnockbackBlocked"; // 用于分支判断
        public string collisionTargetKey = "CollisionTarget"; // 用于连带伤害

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit user = ctx.User;
            Unit target = ctx.TargetUnit;

            // 基础检查：目标必须存在且可移动，自己也得能动
            if (target == null || !target.unitDefinition.beMoved)
            {
                ctx.SetData(resultKey, true); // 推不动视为阻挡
                yield break;
            }
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;

            Battalion attackerBat = user as Battalion;
            Battalion defenderBat = target as Battalion;
            
            // ==========================================
            // 动画阶段 1：聚焦施法者，表现冲撞瞬间
            // ==========================================
            if (camController != null && user != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(user.position), focusDist, camTransitionTime);
            }
            if (attackerBat != null) attackerBat.CurrentSoldierState = SoldierState.Attack;
            if (defenderBat != null) defenderBat.CurrentSoldierState = SoldierState.Hit;
            
            // 等待 t 秒，表现冲锋撞击的瞬间顿帧
            yield return new WaitForSeconds(t);


            // ==========================================
            // 逻辑执行阶段：处理网格位移 (推与跟随)
            // ==========================================
            Vector2Int currentUserPos = user.position;
            Vector2Int currentTargetPos = target.position;
            
            bool isBlocked = false;
            Unit collisionObject = null;
            
            for (int i = 0; i < pushDistance; i++)
            {
                Vector3Int pushDir = Battle.Instance.GetHexDirection(currentUserPos, currentTargetPos);
                
                Vector2Int nextTargetPos = Battle.Instance.GetTileInDirection(currentTargetPos, pushDir, 1);

                if (Battle.Instance.IsValidMapPosition(nextTargetPos) && 
                    Battle.Instance.CanUnitStopOnTile(target, nextTargetPos,false))
                {
                    // 目标后退
                    yield return Battle.Instance.ForceMoveUnitRoutine(target, nextTargetPos);
                    
                    Vector2Int emptySlotPos = currentTargetPos;
                    currentTargetPos = nextTargetPos;
                    
                    // 施法者跟进
                    if (Battle.Instance.CanUnitStopOnTile(user, emptySlotPos,false))
                    {
                        yield return Battle.Instance.ForceMoveUnitRoutine(user, emptySlotPos);
                        currentUserPos = emptySlotPos; 
                    }
                    else
                    {
                        Debug.Log("冲锋中断：目标已击退，但施法者无法跟进。");
                        break; 
                    }
                }
                else
                {
                    if (Battle.Instance.IsValidMapPosition(nextTargetPos))
                    {
                        var tile = Battle.Instance.mapData[nextTargetPos.x, nextTargetPos.y];
                        collisionObject = tile.Battalion != null ? (Unit)tile.Battalion : tile.Facility;
                    }

                    isBlocked = true; 
                    break;
                }
            }
            
            ctx.SetData(resultKey, isBlocked);
            
            if (isBlocked && collisionObject != null)
            {
                ctx.SetData(collisionTargetKey, collisionObject);
            }
            
            // ==========================================
            // 动画阶段 2：追踪目标位移后的新位置
            // ==========================================
            if (camController != null)
            {
                // 因为是跟随冲锋，所以目标的新位置就是此时战场的焦点
                camController.FocusOnPosition(Battle.Instance.MapToWorld(target.position), focusDist, camTransitionTime);
            }

            // 稍微停顿，让镜头和玩家视线稳定在新坐标
            yield return new WaitForSeconds(t);

        }
    }
}