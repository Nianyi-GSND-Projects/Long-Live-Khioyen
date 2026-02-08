using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Effects/Conditional Execution")]
    public class ConditionalEffect : EffectDefinition
    {
        [Header("Condition")]
        [Tooltip("从 Context 中读取的 Key")]
        public string checkKey = "IsKnockbackBlocked";
        
        [Header("True Branch")]
        public List<EffectDefinition> ifTrueEffects = new List<EffectDefinition>();

        [Header("False Branch")]
        public List<EffectDefinition> ifFalseEffects = new List<EffectDefinition>();

        public override void Execute(ActionContext ctx)
        {
            bool conditionValue = ctx.GetData<bool>(checkKey);

            List<EffectDefinition> targetList = conditionValue ? ifTrueEffects : ifFalseEffects;

            foreach (var effect in targetList)
            {
                effect.Execute(ctx);
            }
        }
    }
}