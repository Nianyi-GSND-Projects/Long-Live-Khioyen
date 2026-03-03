using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Battalion : Unit<BattalionDefinition>
    {
        public List<inBattleItem> inventory;
        public GameCommander battalionCommander;
        
        public int currentSoliders
        {
            get => currentHealth;
            set => currentHealth = value;
        }
        public int ArmyId;
        public int currentMurale;
        public int currentTraining;
        public int currentMovement;
        
        public int exp = 0;
        

        public Battalion()
        {
            InstanceId = -1;
            inventory = new List<inBattleItem>();
        }

        public override float GetPower()
        {
            if (entryStats == null) return 0;
            
            float baseVal = entryStats.attackPower;
            
            float ratio = (float)currentSoliders / BattleParam.Instance.defaultSoliderAmountForOnePower;
            
            //TODO：其他修正
            
            return baseVal * ratio;
        }
        
        public override float GetRepairPower()
        {
            if (entryStats == null) return 0;
            
            float baseVal = entryStats.repairPower;
            
            float ratio = (float)currentSoliders / BattleParam.Instance.defaultSoliderAmountForOnePower;
            
            //TODO：其他修正
            
            return baseVal * ratio;
        }
        
        public override float GetDefense()
        {
            if (entryStats == null) return 0;
        
            float baseVal = entryStats.defensePower;
            float ratio = 1.0f;
            //TODO:修正
        
            return baseVal * ratio;
        }
        
        public float CurrentFlexibility
        {
            get
            {
                if (entryStats == null) return 0;
            
                float baseVal = entryStats.flexibility;
            
                float ratio = 1.0f;
                //TODO:修正
        
                return baseVal * ratio;
            }
        }
        
        public int MaxMovement => Mathf.FloorToInt(CurrentFlexibility/BattleParam.Instance.mobilityPerMovement);
        
        public float CurrentDiscipline
        {
            get
            {
                if (entryStats == null) return 0;
                
                float baseVal = entryStats.discipline;
            
                float ratio = 1.0f;
                //TODO:修正
        
                return baseVal * ratio;
            }
        }
        
        public float CurrentStrategy
        {
            get
            {
                if (entryStats == null) return 0;
                float baseVal = entryStats.strategy;
            
                float ratio = 1.0f;
                //TODO:修正
        
                return baseVal * ratio;
            }
        }
        
        
        public override void ApplyBuff(BuffDescriptor buffDescriptor)
        {
            if (buffDescriptor.definition.unitType == BuffUnitType.Battalion ||
                buffDescriptor.definition.unitType == BuffUnitType.Both)
            {
                //处理逻辑
                Buff newBuff = new Buff()
                {
                    descriptor = buffDescriptor,
                    currentDuration = buffDescriptor.defaultDuration
                };
                
                buffs.Add(newBuff);
                
            }
        }
        
        public override void TakeDamage(int rawDamage, Unit attacker = null)
        {
            int oldHealth = currentHealth;
            base.TakeDamage(rawDamage,attacker);
        
            if (currentHealth < oldHealth)
            {
                //TODO:经验值公式 
                AddExp(oldHealth-currentHealth); 
            }
        }
        
        public void AddExp(int amount)
        {
            exp += amount;
            Debug.Log($"Battalion {name} gained {amount} EXP. Total: {exp}");
            // TODO: 战斗内升级？或者只累积到战后结算
        }
        
        public override void OnTurnStart()
        {
            actionDone = false;
            hasMovedThisTurn = false;
            currentMovement = MaxMovement;
            //TODO
        }
        
        public override void CalculateEntryStats(UnitDescriptor desc)
        {
            base.CalculateEntryStats(desc);
        
            if (desc is not BattalionDescriptor batDesc) return;

            // 1. 基础值
            float baseAttack = Definition.defaultPower;
            float baseDef = Definition.defaultDefense;
            float baseRepair = Definition.defaultRepairPower;
            
            float baseFlex = batDesc.flexibility > 0 ? batDesc.flexibility : Definition.defaultFlexibility;
            float baseDisc = batDesc.discipline > 0 ? batDesc.discipline : Definition.defaultDiscipline; 
            float baseStrat = batDesc.strategy > 0 ? batDesc.strategy : Definition.defaultStrategy; 
            
            // 2. 指挥官属性修正
            float commanderAttackBonus = 0;
            float commanderDefBonus = 0;
            float commanderFlexBonus = 0;
            float commanderDiscBonus = 0;
            float commanderStratBonus = 0;
            
            if (battalionCommander != null)
            {
                var param = BattleParam.Instance;
                commanderAttackBonus = CalculateBonus(param.attackScaling);
                commanderDefBonus = CalculateBonus(param.defenseScaling);
                commanderFlexBonus = CalculateBonus(param.mobilityScaling); // 对应 Flexibility
                commanderDiscBonus = CalculateBonus(param.disciplineScaling);
                commanderStratBonus = CalculateBonus(param.strategyScaling);
            }

            //3. 科技修正
            //4.指挥官技能修正
            entryStats.maxHealth = batDesc.maxSolider;
            entryStats.maxMorale = batDesc.maxMorale;
            
            entryStats.attackPower = baseAttack + commanderAttackBonus;
            entryStats.defensePower = baseDef + commanderDefBonus;
            entryStats.repairPower = baseRepair;
            entryStats.flexibility = baseFlex + commanderFlexBonus;
            entryStats.discipline = baseDisc + commanderDiscBonus;
            entryStats.strategy = baseStrat + commanderStratBonus;
            //设置初始状态
            currentHealth = batDesc.currentSoliders; // 从 Descriptor 读取初始兵力
            currentMurale = batDesc.currentMurale;
            currentTraining = batDesc.currentTraining;
            
            currentMovement = MaxMovement;
            Debug.Log($"[Stats] Battalion {name} Entry Stats Calculated.");
        }
        
        float CalculateBonus(CommanderStatScaling scaling)
        {
            if (scaling == null) return 0;
            return battalionCommander.Zhi * scaling.zhiFactor +
                   battalionCommander.Xin * scaling.xinFactor +
                   battalionCommander.Ren * scaling.renFactor +
                   battalionCommander.Yong * scaling.yongFactor +
                   battalionCommander.Yan * scaling.yanFactor;
        }
    }
    
    
}
