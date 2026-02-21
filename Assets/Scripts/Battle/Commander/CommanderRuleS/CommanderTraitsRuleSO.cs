using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Rules/Traits Rule")]
    public class CommanderTraitsRuleSO : ScriptableObject
    {
        public string ruleName; // e.g., "Aggressive", "Cautious"

        // TODO: 定义技能池、性格池
        
        public void ApplyTraits(GameCommander commander)
        {
            // TODO: 随机分配技能和性格
            Debug.Log($"Applying traits from rule: {ruleName}");
        }
    }
}