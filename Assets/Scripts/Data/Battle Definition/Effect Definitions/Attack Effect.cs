using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Effect Definition/Attack")]
    public class AttackEffect : EffectDefinition
    {
        [Header("Parameter")]
        public float multiplier = 1.0f;//攻击力倍率
        public int minDamage = 1;
        public bool beCountered = true;
        public bool canDamageFacility = true;
        
        public GameObject hitEffect;
        
        public override void Execute(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            
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
            

            target.TakeDamage((int)(multiplier * ctx.User.GetPower()));
                
            if(beCountered) ctx.User.TakeDamage((int)(multiplier * target.GetPower()));//反击生效
                
            if(hitEffect != null) Instantiate(hitEffect, target.transform.position, Quaternion.identity);

        }
    }
}
