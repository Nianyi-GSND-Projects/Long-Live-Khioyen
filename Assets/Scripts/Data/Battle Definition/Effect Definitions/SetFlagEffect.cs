using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Set Flag")]
    public class SetFlagEffect : EffectDefinition
    {
        [Header("Settings")]
        [Tooltip("要设置的上下文 Key")]
        public string flagKey = "IsSurpriseAttack";

        [Tooltip("要设置的值 (True/False)")]
        public bool valueToSet = true;

        [Header("Conditional (Optional)")]
        [Tooltip("是否启用条件判断？如果不启用，则无条件设置。")]
        public bool useCondition = false;
        
        [Tooltip("概率 (0-100)，仅在 useCondition=true 时生效")]
        [Range(0, 100)]
        public int chancePercentage = 100;

        public override void Execute(ActionContext ctx)
        {
            // 如果启用了条件，先掷骰子
            if (useCondition)
            {
                int roll = Random.Range(0, 100);
                if (roll >= chancePercentage)
                {
                    // 判定失败，不设置 Flag (或者你可以选择设置为 !valueToSet)
                    return; 
                }
            }

            // 设置值
            ctx.SetData(flagKey, valueToSet);
            // Debug.Log($"[Effect] SetFlag {flagKey} = {valueToSet}");
        }
    }
}