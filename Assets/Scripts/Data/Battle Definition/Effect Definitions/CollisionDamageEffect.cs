using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Collision Damage")]
    public class CollisionDamageEffect : EffectDefinition
    {
        [Header("Settings")]
        public string targetKey = "CollisionTarget";
        
        [Header("Input Damage")]
        public bool useDamageFromContext = true;
        public string inputDamageKey = "LastDamageAmount";
        public float fallbackDamage = 0; // 如果没读到伤害，用这个

        public override void Execute(ActionContext ctx)
        {
            Unit victim = ctx.GetData<Unit>(targetKey);
            if (victim == null) return;

            int damageToApply = (int)fallbackDamage;
            
            if (useDamageFromContext)
            {
                object val = ctx.GetData<object>(inputDamageKey);
                if (val != null)
                {
                    damageToApply = System.Convert.ToInt32(val);
                }
                
                Debug.Log($"{victim.name} 受到连带伤害: {damageToApply}");
                victim.TakeDamage(damageToApply);
            }
        }
    }
}