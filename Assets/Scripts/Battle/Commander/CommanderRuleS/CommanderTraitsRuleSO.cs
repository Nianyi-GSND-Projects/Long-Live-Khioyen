using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Rules/Traits Rule")]
    public class CommanderTraitsRuleSO : ScriptableObject
    {
        public string ruleName;

        [Header("Level Settings")]

        [Header("Pools")]
        public List<CommanderPersonalitySO> possiblePersonalities;
        public List<CommanderTraitSO> possibleTraits;
        public List<ActionDefinition> possibleActions;

        public void ApplyTraits(GameCommander commander,int level)
        {

            // 1. 随机性格 (必选 1 个)
            if (possiblePersonalities != null && possiblePersonalities.Count > 0)
            {
                commander.personality = possiblePersonalities[Random.Range(0, possiblePersonalities.Count)];
            }

            // 2. 随机特性 (数量 = Level)
            if (possibleTraits != null && possibleTraits.Count > 0)
            {
                commander.traits = PickRandomUnique(possibleTraits, commander.level);
            }

            // 3. 随机行动 (数量 = Level)
            if (possibleActions != null && possibleActions.Count > 0)
            {
                commander.commanderActions = PickRandomUnique(possibleActions, commander.level);
            }
        }

        // 辅助方法：从列表中随机抽取 N 个不重复的项
        private List<T> PickRandomUnique<T>(List<T> source, int count)
        {
            List<T> result = new List<T>();
            List<T> pool = new List<T>(source); // 复制一份以免修改原列表

            int pickCount = Mathf.Min(count, pool.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index); // 移除已选，防止重复
            }
            return result;
        }
    }
}