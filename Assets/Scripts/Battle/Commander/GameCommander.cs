using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public enum Race
    {
        Han,
        Xiong
    }
    [System.Serializable]
    public class GameCommander
    {
        public int commanderId;
        public string commanderName;
        public Sprite portrait;
        
        [Header("Identity")]
        public Race race;
        public int level = 1;
        
        [Header("Stats")]
        public int Zhi;
        public int Xin;
        public int Ren;
        public int Yong;
        public int Yan;

        public bool isAssigned;

        [Header("Traits & Skills")]
        public CommanderPersonalitySO personality;
        public List<CommanderTraitSO> traits = new List<CommanderTraitSO>();
        public List<ActionDefinition> commanderActions = new List<ActionDefinition>();

        #region LevelUp
        
        public void LevelUp()
        {
            if (personality == null)
            {
                Debug.LogWarning($"Commander {commanderName} has no personality, skipping stat growth.");
                level++;
                return;
            }

            // 1. 固定成长 (5 点)
            for (int i = 0; i < 5; i++)
            {
                ApplyWeightedGrowth();
            }

            // 2. 额外成长 (概率判定)
            ApplyExtraGrowth();

            level++;
            Debug.Log($"Commander {commanderName} leveled up to {level}!");
        }
        
        private void ApplyWeightedGrowth()
        {
            int totalWeight = personality.TotalWeight;
            if (totalWeight <= 0) return;

            int roll = Random.Range(0, totalWeight);
            int current = 0;

            // 智
            current += personality.zhiWeight;
            if (roll < current) { Zhi++; return; }

            // 信
            current += personality.xinWeight;
            if (roll < current) { Xin++; return; }

            // 仁
            current += personality.renWeight;
            if (roll < current) { Ren++; return; }

            // 勇
            current += personality.yongWeight;
            if (roll < current) { Yong++; return; }

            // 严
            current += personality.yanWeight;
            if (roll < current) { Yan++; return; }
        }

        private void ApplyExtraGrowth()
        {
            if (Random.Range(0, 100) < personality.zhiChance) Zhi++;
            if (Random.Range(0, 100) < personality.xinChance) Xin++;
            if (Random.Range(0, 100) < personality.renChance) Ren++;
            if (Random.Range(0, 100) < personality.yongChance) Yong++;
            if (Random.Range(0, 100) < personality.yanChance) Yan++;
        }
        

        #endregion
   }
}
