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

        public override void Execute(ActionContext ctx)
        {
            Unit user = ctx.User;
            Unit target = ctx.TargetUnit;

            // 基础检查：目标必须存在且可移动，自己也得能动
            if (target == null || !target.unitDefinition.beMoved)
            {
                ctx.SetData(resultKey, true); // 推不动视为阻挡
                return;
            }

            Vector2Int currentUserPos = user.position;
            Vector2Int currentTargetPos = target.position;
            
            bool isBlocked = false;
            Unit collisionObject = null;
            
            for (int i = 0; i < pushDistance; i++)
            {
                Vector3Int pushDir = Battle.Instance.GetHexDirection(currentUserPos, currentTargetPos);
                
                Vector2Int nextTargetPos = Battle.Instance.GetTileInDirection(currentTargetPos, pushDir, 1);


                if (Battle.Instance.IsValidMapPosition(nextTargetPos) && 
                    Battle.Instance.CanUnitStopOnTile(target, nextTargetPos))
                {

                    Battle.Instance.ForceMoveUnit(target, nextTargetPos);
                    
                    Vector2Int emptySlotPos = currentTargetPos;
                    
                    currentTargetPos = nextTargetPos;
                    
                    if (Battle.Instance.CanUnitStopOnTile(user, emptySlotPos))
                    {
                        Battle.Instance.ForceMoveUnit(user, emptySlotPos);
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
        }
    }
}