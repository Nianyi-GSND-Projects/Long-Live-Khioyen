// Assets/Scripts/Battle/Data/BattlePoolSO.cs (新文件)

using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class WeightedBattlePreset
    {
        public BattlePresetSO preset;
        [Min(1)] public int weight = 10;
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Level/Battle Pool")]
    public class BattlePoolSO : ScriptableObject
    {
        [Header("Battle Presets")]
        public List<WeightedBattlePreset> battlePresets = new List<WeightedBattlePreset>();

        public BattlePresetSO GetRandomBattlePreset()
        {
            if (battlePresets == null || battlePresets.Count == 0)
            {
                Debug.LogWarning($"BattlePool {name} is empty!");
                return null;
            }

            int totalWeight = 0;
            foreach (var item in battlePresets)
            {
                if (item.preset != null)
                    totalWeight += item.weight;
            }

            if (totalWeight == 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var item in battlePresets)
            {
                if (item.preset == null) continue;

                currentWeight += item.weight;
                if (randomValue < currentWeight)
                {
                    return item.preset;
                }
            }

            return battlePresets[0].preset;
        }
    }
}