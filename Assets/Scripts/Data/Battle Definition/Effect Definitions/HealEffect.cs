using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    public enum HealScalingSource
    {
        None,           // 固定值
        Power,          // 基于攻击力
        RepairPower     // 基于修补力
    }
    
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Heal")]
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
        
        [Header("Visuals")]
        public GameObject vfxPrefab;
        [Tooltip("特效生成时的垂直高度偏移，确保特效出现在单位头顶")]
        public float vfxHeightOffset = 1.5f;

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            Unit user = ctx.User;

            if (target == null) yield break; 

            if (target is Battalion && !canHealBattalion) yield break;
            if (target is Facility && !canHealFacility) yield break;
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;
            
            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;
            
            
            Battalion casterBat = user as Battalion;
            
            // ==========================================
            // 动画阶段 1：聚焦施法者，播放 Cast 动作
            // ==========================================
            if (camController != null && user != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(user.position), focusDist, camTransitionTime);
            }
            if (casterBat != null) casterBat.CurrentSoldierState = SoldierState.Cast; 

            yield return new WaitForSeconds(t);

            // ==========================================
            // 动画阶段 2：聚焦目标，施加治疗并播放特效
            // ==========================================
            if (camController != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(target.position), focusDist, camTransitionTime);
            }

            if (vfxPrefab != null)
            {
                Vector3 spawnPos = target.transform.position + Vector3.up * vfxHeightOffset;
                Instantiate(vfxPrefab, spawnPos, Quaternion.identity);
            }

            float finalHeal = baseHealAmount;
            if (user != null)
            {
                float bonus = 0;
                switch (scalingSource)
                {
                    case HealScalingSource.Power: bonus = user.GetStat(StatType.AttackPower); break;
                    case HealScalingSource.RepairPower: bonus = user.GetStat(StatType.RepairPower); break;
                }
                finalHeal += bonus * scalingFactor;
            }

            int amount = Mathf.FloorToInt(finalHeal);
            if (amount > 0)
            {
                target.Heal(amount);
                Debug.Log($"{user?.name} healed {target.name} for {amount}");
            }
            
            if (Battle.Instance != null) Battle.Instance.MarkUnitDirty(target);

            yield return new WaitForSeconds(t);
        }
    }
}