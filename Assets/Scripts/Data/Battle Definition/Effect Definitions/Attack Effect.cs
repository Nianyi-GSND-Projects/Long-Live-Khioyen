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
        
        public override void Execute(ActionContext ctx)
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
                    return;
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
                return;
            }
            
            if (target is Facility && !canDamageFacility)
            {
                Debug.Log("该技能无法伤害设施。");
                return;
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
            
            if (isSurprise)
            {
                Debug.Log("<color=yellow>[偷袭触发]</color> 优先结算攻击伤害！");
                
                ApplyDamage(ctx, ctx.User, target, damageToDefender);
                
                bool targetCanFight = (target is Battalion bat && bat.currentSoliders > 0) || 
                                      (target is Facility fac && fac.currentDurability > 0);

                if (beCountered && targetCanFight)
                {
                    int damageToAttacker = CalculateCounterDamage(target);
                    ApplyDamage(ctx, target, ctx.User, damageToAttacker, isCounter: true);
                }
            }
            else
            {
                int damageToAttacker = 0;
                if (beCountered)
                {
                    damageToAttacker = CalculateCounterDamage(target);
                }

                ApplyDamage(ctx, ctx.User, target, damageToDefender);
                
                if (beCountered && damageToAttacker > 0)
                {
                    ApplyDamage(ctx, target, ctx.User, damageToAttacker, isCounter: true);
                }
            }
            
            if(hitEffect != null) Instantiate(hitEffect, target.transform.position, Quaternion.identity);

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
