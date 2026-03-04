using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        
        [Header("StatsBase")]
        //自身面板数值,不考虑任何装备与加成,仅自身基础值与升级数值
        public int Zhi;
        public int Xin;
        public int Ren;
        public int Yong;
        public int Yan;
        
        public bool isAssigned;

        [Header("Equipments")]
        public EquipmentDefinition[] equipments = new EquipmentDefinition[3];
        
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
        
        #region Stat Getters (Calculated)
        
        public int GetTotalZhi()
        {
            int val = Zhi;
            val += GetEquipmentBonus(e => e is StatBonusEffect s ? s.zhiBonus : 0);
            return Mathf.Max(0, val);
        }

        public int GetTotalXin()
        {
            int val = Xin;
            val += GetEquipmentBonus(e => e is StatBonusEffect s ? s.xinBonus : 0);
            return Mathf.Max(0, val);
        }

        public int GetTotalRen()
        {
            int val = Ren;
            val += GetEquipmentBonus(e => e is StatBonusEffect s ? s.renBonus : 0);
            return Mathf.Max(0, val);
        }

        public int GetTotalYong()
        {
            int val = Yong;
            val += GetEquipmentBonus(e => e is StatBonusEffect s ? s.yongBonus : 0);
            return Mathf.Max(0, val);
        }

        public int GetTotalYan()
        {
            int val = Yan;
            val += GetEquipmentBonus(e => e is StatBonusEffect s ? s.yanBonus : 0);
            return Mathf.Max(0, val);
        }
        
        public int GetMaxSoldiersBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.maxSoldiersBonus : 0);
        }

        public int GetMaxMoraleBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.maxMoraleBonus : 0);
        }

        public int GetFlexibilityBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.flexibilityBonus : 0);
        }

        public int GetDisciplineBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.disciplineBonus : 0);
        }

        public int GetStrategyBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.strategyBonus : 0);
        }
        
        public int GetPowerBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.attackBonus : 0);
        }

        public int GetDefenceBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.defenceBonus : 0);
        }

        public int GetZocBonus()
        {
                return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.zocPowerBonus : 0);
        }
        public int GetMovementBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.movementBonus : 0);
        }

        public int GetActionChanceBonus()
        {
            return GetEquipmentBonus(e => e is BattalionBonusEffect b ? b.actionChanceBonus : 0);
        }

        private int GetEquipmentBonus(System.Func<EquipmentEffect, int> selector)
        {
            int bonus = 0;
            //TODO
            
            return bonus;
        }
        
        public List<ActionDefinition> GetAllActions()
        {
            HashSet<ActionDefinition> allActions = new HashSet<ActionDefinition>(commanderActions);
            //TODO

            return allActions.ToList();
        }
        
        #endregion
   }
}
