using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class LootItemEntry
    {
        public ItemDefinition item;
        [Min(1)] public int weight = 10;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public List<LootItemEntry> entries = new List<LootItemEntry>();

        public inBattleItem Roll()
        {
            if (entries == null || entries.Count == 0) return null;

            // 1. 计算总权重
            int totalWeight = 0;
            foreach (var entry in entries) totalWeight += entry.weight;

            // 2. 随机 Roll
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (var entry in entries)
            {
                current += entry.weight;
                if (roll < current)
                {
                    // 3. 生成物品
                    int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);
                    return new inBattleItem { definition = entry.item, amount = amount };
                }
            }
            return null;
        }
    }
}