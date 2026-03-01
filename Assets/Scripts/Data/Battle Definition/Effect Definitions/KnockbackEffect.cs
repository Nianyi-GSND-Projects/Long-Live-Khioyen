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

        public override void Execute(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            if (target == null || !target.unitDefinition.beMoved) // 假设设施不可被击退
            {
                ctx.SetData(resultKey, true); // 视为被阻挡
                return;
            }

            Vector2Int startPos = ctx.User.position;
            Vector2Int targetPos = target.position;
            Vector2Int currentPos = target.position;
            Vector3Int pushDir = Battle.Instance.GetHexDirection(ctx.User.position, currentPos);
            
            bool blocked = false;
            Unit collisionObject = null; // 记录撞到了谁

            for (int i = 1; i <= pushDistance; i++)
            {
                Vector2Int nextPos = Battle.Instance.GetTileInDirection(currentPos, pushDir, 1);
                
                if (Battle.Instance.IsValidMapPosition(nextPos) &&
                    Battle.Instance.CanUnitStopOnTile(target, nextPos,false))
                {
                    // 移动一步
                    Battle.Instance.ForceMoveUnit(target, nextPos);
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
        }
    }
}