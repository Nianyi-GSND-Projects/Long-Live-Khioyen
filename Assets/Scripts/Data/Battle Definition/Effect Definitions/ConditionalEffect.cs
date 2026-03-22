using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Conditional Execution")]
    public class ConditionalEffect : EffectDefinition
    {
        [Header("Condition")]
        [Tooltip("从 Context 中读取的 Key")]
        public string checkKey = "IsKnockbackBlocked";
        
        [Header("True Branch")]
        public List<EffectDefinition> ifTrueEffects = new List<EffectDefinition>();

        [Header("False Branch")]
        public List<EffectDefinition> ifFalseEffects = new List<EffectDefinition>();

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            // 读取分支条件
            bool conditionValue = ctx.GetData<bool>(checkKey);

            List<EffectDefinition> targetList = conditionValue ? ifTrueEffects : ifFalseEffects;

            // 依次执行符合条件的分支内的所有 Effect
            foreach (var effect in targetList)
            {
                if (effect != null)
                {
                    yield return effect.ExecuteCoroutine(ctx);
                }
            }
        }
    }
}