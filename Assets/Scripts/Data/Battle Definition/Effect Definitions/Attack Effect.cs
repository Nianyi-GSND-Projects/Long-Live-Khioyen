using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Attack")]
    public class AttackEffect : EffectDefinition
    {
        [Header("Parameter")]
        public float multiplier = 1.0f;//攻击力倍率
        public int minDamage = 1;
        public bool beCountered = true;
        public bool canDamageFacility = true;
        
        public bool supportSurprise = true;
        public string surpriseFlagKey = "IsSurpriseAttack";
        
        [Header("Targeting Mode")]
        [Tooltip("如果勾选，将追踪最初选中的那个单位（即使它移动了）。\n如果不勾选，将攻击目标地块上当前存在的单位。")]
        public bool lockOriginalTarget = true;
        
        [Header("Visual")]
        public GameObject hitEffect;
        
        [Header("Output")]
        public string outputDamageKey = "LastDamageAmount";
        
        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit target = null;
            if (lockOriginalTarget)
            {
                // 追踪模式：使用 Context 中缓存的初始引用
                target = ctx.OriginalTargetUnit;
                
                // 额外检查：如果目标已经死了/被销毁了，就没法打了
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    Debug.Log("原定目标已丢失 (死亡或销毁)，攻击失效。");
                    yield break;
                }
            }
            else
            {
                // 地块模式：去目标坐标重新找人
                // 这适用于"对地轰炸"，如果人跑了，炸的就是空地（target=null）
                // 或者如果人跑了，另一个人走过来了，炸的就是新来的人
                target = ctx.TargetUnit; // 使用属性访问 mapData[TargetPos]
            }
            
            if (target == null)
            {
                Debug.Log($"攻击打在了空地 {ctx.TargetPos} 上。");
                yield break;
            }
            
            if (target is Facility && !canDamageFacility)
            {
                Debug.Log("该技能无法伤害设施。");
                yield break;
            }
            
            bool isSurprise = false;
            if (supportSurprise)
            {
                // 读取上下文中的 Flag，默认为 false
                object val = ctx.GetData<object>(surpriseFlagKey);
                if (val != null) isSurprise = (bool)val;
            }
            
            float basePower = ctx.User.GetPower();
            int damageToDefender = Mathf.CeilToInt(basePower * multiplier);
            damageToDefender = Mathf.Max(minDamage, damageToDefender);
            
            int damageToAttacker = 0;
            bool willCounter = false;
            
            if (!isSurprise && beCountered)
            {
                // 常规攻击：在受击前预先计算反击伤害
                damageToAttacker = CalculateCounterDamage(target);
                if (damageToAttacker > 0) willCounter = true;
            }
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;
            bool showVisuals = false;
            
            if (Battle.Instance != null)
            {
                bool userVis = ctx.User != null && Battle.Instance.IsUnitVisibleToPlayer(ctx.User);
                bool targetVis = target != null && Battle.Instance.IsUnitVisibleToPlayer(target);
                showVisuals = userVis || targetVis;
            }
            
            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
            {
                camController = Battle.Instance.inputController.cameraController;
            }
            
            Battalion attackerBat = ctx.User as Battalion;
            Battalion defenderBat = target as Battalion;
            Vector3 attackerWorldPos = Battle.Instance.MapToWorld(ctx.User.position);
            Vector3 targetWorldPos = Battle.Instance.MapToWorld(target.position);

            if (showVisuals)
            {
                if (camController != null) 
                {
                    camController.FocusOnPosition(attackerWorldPos, focusDist, camTransitionTime);
                }
                yield return new WaitForSeconds(t);
            }
            
            // ==========================================
            // 动画阶段 1：攻击与受击
            // ==========================================
            if (showVisuals)
            {
                if (camController != null) 
                {
                    camController.FocusOnPosition(targetWorldPos, focusDist, camTransitionTime);
                }
                if (attackerBat != null) attackerBat.CurrentSoldierState = SoldierState.Attack;
                if (defenderBat != null) defenderBat.CurrentSoldierState = SoldierState.Hit;
            
                if(hitEffect != null) Instantiate(hitEffect, target.transform.position, Quaternion.identity);
            
                yield return new WaitForSeconds(t);
            }
            // 等待时间结束，正式结算伤害
            ApplyDamage(ctx, ctx.User, target, damageToDefender);
            
            // ==========================================
            // 动画阶段 2：反击逻辑判定与表现
            // ==========================================
            if (isSurprise && beCountered)
            {
                // 偷袭攻击：在受击后，检查存活情况以决定是否反击
                bool targetCanFight = (target is Battalion bat && bat.currentSoliders > 0) || 
                                      (target is Facility fac && fac.currentDurability > 0);
                if (targetCanFight)
                {
                    damageToAttacker = CalculateCounterDamage(target);
                    if (damageToAttacker > 0) willCounter = true;
                }
            }

            if (willCounter)
            {
                if (showVisuals)
                {
                    if (camController != null) 
                    {
                        camController.FocusOnPosition(attackerWorldPos, focusDist, camTransitionTime);
                    }
                    // 角色互换，防守方攻击，进攻方受击
                    if (defenderBat != null) defenderBat.CurrentSoldierState = SoldierState.Attack;
                    if (attackerBat != null) attackerBat.CurrentSoldierState = SoldierState.Hit;
                
                    if(hitEffect != null) Instantiate(hitEffect, ctx.User.transform.position, Quaternion.identity);

                    yield return new WaitForSeconds(t);
                }
                
                ApplyDamage(ctx, target, ctx.User, damageToAttacker, isCounter: true);
            }
            
            // ==========================================
            // 动画阶段 3：动作结束，重置待机
            // ==========================================
            if (attackerBat != null) attackerBat.CurrentSoldierState = SoldierState.Idle;
            if (defenderBat != null) defenderBat.CurrentSoldierState = SoldierState.Idle;
        }
        
        private void ApplyDamage(ActionContext ctx, Unit from, Unit to, int amount, bool isCounter = false)
        {
            if (amount <= 0) return;

            string prefix = isCounter ? "[反击]" : "[攻击]";
            Debug.Log($"{prefix} {from.name} -> {to.name} (伤害: {amount})");
            
            to.TakeDamage(amount,from);
            
            if (from is Battalion attackerBat)
            {
                attackerBat.AddExp(amount);
            }
            
            if (!isCounter && !string.IsNullOrEmpty(outputDamageKey))
            {
                ctx.SetData(outputDamageKey, amount);
            }
        }
        
        private int CalculateCounterDamage(Unit defender)
        {
            float defenderPower = defender.GetPower();
            int dmg = Mathf.CeilToInt(defenderPower);
            return Mathf.Max(0, dmg);
        }
    }
}
