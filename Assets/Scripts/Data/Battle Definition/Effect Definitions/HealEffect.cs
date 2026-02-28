using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Heal")]
    public class HealEffect : EffectDefinition
    {
        public int healAmount = 10;
        public bool canHealBattalion = true;
        public bool canHealFacility = true;
        
        public GameObject vfxPrefab;

        public override void Execute(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            if (target == null) return;

            if (target is Battalion && !canHealBattalion) return;
            if (target is Facility && !canHealFacility) return;

            target.Heal(healAmount);
            
            if (vfxPrefab != null) Instantiate(vfxPrefab, target.transform.position, Quaternion.identity);
            
            // 标记刷新
            if (Battle.Instance != null) Battle.Instance.MarkUnitDirty(target);
        }
    }
}