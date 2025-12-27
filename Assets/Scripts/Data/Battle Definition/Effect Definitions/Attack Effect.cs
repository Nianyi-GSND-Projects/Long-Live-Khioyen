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
        
        public GameObject hitEffect;
        
        public override void Execute(ActionContext context)
        {
            if (context.Target != null)
            {
                context.Target.TakeDamage((int)(multiplier * context.User.GetPower()));
                
                if(beCountered) context.User.TakeDamage((int)(multiplier * context.Target.GetPower()));//反击生效
                
                if(hitEffect != null) Instantiate(hitEffect, context.Target.transform.position, Quaternion.identity);
            }
        }
    }
}
