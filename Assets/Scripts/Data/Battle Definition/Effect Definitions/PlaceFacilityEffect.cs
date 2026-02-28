using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Place Facility")]
    public class PlaceFacilityEffect : EffectDefinition
    {
        public override void Execute(ActionContext ctx)
        {
            if (Battle.Instance == null) return;

            // 直接调用 Battle 的方法
            Unit facility = Battle.Instance.BuildPendingFacility(ctx.TargetPos, ctx.User.faction);
            
            if (facility != null)
            {
                Debug.Log($"Built {facility.name} at {ctx.TargetPos}");
            }
        }
    }
}