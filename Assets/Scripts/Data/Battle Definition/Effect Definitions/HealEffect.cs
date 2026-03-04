using UnityEngine;

namespace LongLiveKhioyen
{
    public enum HealScalingSource
    {
        None,           // 固定值
        Power,          // 基于攻击力
        RepairPower     // 基于修补力
    }
    public class HealEffect : EffectDefinition
    {
        [Header("Heal Settings")]
        public int baseHealAmount = 0;
        [Tooltip("治疗量的加成来源")]
        public HealScalingSource scalingSource = HealScalingSource.None;
        [Tooltip("属性加成的倍率 (默认为 1.0)")]
        public float scalingFactor = 1.0f;
        
        public bool canHealBattalion = true;
        public bool canHealFacility = true;
        
        public GameObject vfxPrefab;

        public override void Execute(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            Unit user = ctx.User; // 获取来源单位

            if (target == null) return;

            if (target is Battalion && !canHealBattalion) return;
            if (target is Facility && !canHealFacility) return;

            // 计算治疗量
            float finalHeal = baseHealAmount;

            if (user != null)
            {
                float bonus = 0;
                switch (scalingSource)
                {
                    case HealScalingSource.Power:
                        bonus = user.GetStat(StatType.AttackPower);
                        break;
                    case HealScalingSource.RepairPower:
                        bonus = user.GetStat(StatType.RepairPower);
                        break;
                }
                finalHeal += bonus * scalingFactor;
            }

            // 取整并应用
            int amount = Mathf.FloorToInt(finalHeal);
            if (amount > 0)
            {
                target.Heal(amount);
                Debug.Log($"{user?.name} healed {target.name} for {amount} (Base: {baseHealAmount}, Bonus: {finalHeal - baseHealAmount})");
            }
            
            
            // 标记刷新
            if (Battle.Instance != null) Battle.Instance.MarkUnitDirty(target);
        }
    }
}