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
            float ratio = (float)currentSoliders / entryStats.maxHealth;
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
        
        public int MaxMovement => Mathf.FloorToInt(CurrentFlexibility)/10;
        
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
            int rawFlex = batDesc.flexibility > 0 ? batDesc.flexibility : Definition.defaultFlexibility;
            int rawDisc = batDesc.discipline > 0 ? batDesc.discipline : Definition.defaultDiscipline; // 假设有
            int rawStrat = batDesc.strategy > 0 ? batDesc.strategy : Definition.defaultStrategy; // 假设有

            // 2. 指挥官属性修正
            //3. 科技修正
            //4.指挥官技能修正
            //TODO
            entryStats.maxHealth = batDesc.maxSolider;
            entryStats.maxMorale = batDesc.maxMorale;
            
            entryStats.defensePower = baseDef;
            entryStats.attackPower = baseAttack;
            entryStats.discipline = rawDisc;
            entryStats.strategy = rawStrat;
            entryStats.flexibility = rawFlex;
            //设置初始状态
            currentHealth = batDesc.currentSoliders; // 从 Descriptor 读取初始兵力
            currentMurale = batDesc.currentMurale;
            currentTraining = batDesc.currentTraining;
            Debug.Log($"[Stats] Battalion {name} Entry Stats Calculated.");
        }
    }
    
    
}
