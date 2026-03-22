using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Battalion : Unit<BattalionDefinition>
    {
        public GameCommander battalionCommander;
        
        public int currentSoliders
        {
            get => currentHealth;
            set => currentHealth = value;
        }
        public int ArmyId;
        public int currentMorale;
        public int currentExp;
        public int currentMovement;
        public bool doubleAction = false;
        public int ExtraMovement = 0;
        public int exp = 0;
        
        private SoldierState _currentSoldierState = SoldierState.Idle;
        public SoldierState CurrentSoldierState
        {
            get => _currentSoldierState;
            set
            {
                // 如果状态相同，则不进行多余的更新
                if (_currentSoldierState != value)
                {
                    _currentSoldierState = value;
                    UpdateVisualState(); // 状态改变时，自动触发视觉更新
                }
            }
        }

        public Battalion()
        {
            InstanceId = -1;
        }
        
        public override void OnTurnStart()
        {
            base.OnTurnStart();
            
            currentMovement = MaxMovement;
            //TODO
        }
        
        public override void OnTurnEnd()
        {
            base.OnTurnEnd();
            
            ConsumeMoralePerTurn();

            if (currentMorale <= 0) MoraleBreak();
        }
        
        public override float GetPower()
        {
            float finalStatPower = GetStat(StatType.AttackPower);

            float soldierRatio = (float)currentSoliders / BattleParam.Instance.defaultSoliderAmountForOnePower;

            return finalStatPower * soldierRatio;
        }
        
        public override float GetRepairPower()
        {
            float finalStatRepair = GetStat(StatType.RepairPower);

            float soldierRatio = (float)currentSoliders / BattleParam.Instance.defaultSoliderAmountForOnePower;

            return finalStatRepair * soldierRatio;
        }
        
        
        public float CurrentFlexibility
        {
            get
            {
               return GetStat(StatType.Flexibility);
            }
        }
        
        public int MaxMovement => Mathf.FloorToInt(CurrentFlexibility/BattleParam.Instance.mobilityPerMovement) + ExtraMovement;
        
        public float CurrentDiscipline
        {
            get
            {
                return GetStat(StatType.Discipline);
            }
        }
        
        public float CurrentStrategy
        {
            get
            {
                return GetStat(StatType.Strategy);
            }
        }
        
        
        public override void ApplyBuff(BuffDescriptor buffDescriptor)
        {
            if (buffDescriptor.definition.unitType == BuffUnitType.Battalion ||
                buffDescriptor.definition.unitType == BuffUnitType.Both)
            {
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
        
        
        
        public override void CalculateEntryStats(UnitDescriptor desc)
        {
            base.CalculateEntryStats(desc);
            if (desc is not BattalionDescriptor batDesc) return;
            
            var definition = Definition;
            var commander = battalionCommander;
            var param = BattleParam.Instance;
            
            // 1. 基础值
            float baseAttack = definition.defaultPower;
            float baseDef = definition.defaultDefense;
            float baseRepair = definition.defaultRepairPower;
            
            float baseFlex = definition.defaultFlexibility;
            float baseDisc = definition.defaultDiscipline; 
            float baseStrat = definition.defaultStrategy; 

            
            float commanderAttackBonus = 0;
            float commanderDefBonus = 0;
            float commanderFlexBonus = 0;
            float commanderDiscBonus = 0;
            float commanderStratBonus = 0;
            int commanderZocBonus = 0;
            int commanderMovementBonus = 0;
            int commanderVisionBonus = 0;
            int commanderMoraleConsumptionBonus = 0;
            
            if (commander != null)
            {
                // 2. 指挥官五维属性加成
                commanderAttackBonus = CalculateBonus(param.attackScaling);
                commanderDefBonus = CalculateBonus(param.defenseScaling);
                commanderFlexBonus = CalculateBonus(param.mobilityScaling);
                commanderDiscBonus = CalculateBonus(param.disciplineScaling);
                commanderStratBonus = CalculateBonus(param.strategyScaling);
                
                //3.指挥官技能与特性带来的直接数值加成
                commanderAttackBonus *= commander.GetPowerBonus();
                commanderDefBonus *= commander.GetDefenceBonus();
                commanderFlexBonus *= commander.GetFlexibilityBonus();
                commanderDiscBonus *= commander.GetDisciplineBonus();
                commanderStratBonus *= commander.GetStrategyBonus();
                commanderMovementBonus += commander.GetMovementBonus();
                commanderZocBonus += commander.GetZocBonus();
                commanderVisionBonus += commander.GetVisionBonus();
                commanderMoraleConsumptionBonus += commander.GetMoraleConsumptionBonus();
            }
            
            
            
            //5.状态参数应用描述符数据
            entryStats.maxHealth = batDesc.maxSolider;
            entryStats.maxMorale = batDesc.maxMorale;
            currentHealth = batDesc.currentSoliders; 
            currentMorale = batDesc.currentMorale;
            currentExp = batDesc.currentExp;
            
            //5.应用数值修改
            entryStats.attackPower = baseAttack + commanderAttackBonus;
            entryStats.defensePower = baseDef + commanderDefBonus;
            entryStats.repairPower = baseRepair;
            entryStats.flexibility = baseFlex + commanderFlexBonus;
            entryStats.discipline = baseDisc + commanderDiscBonus;
            entryStats.strategy = baseStrat + commanderStratBonus;
            
            //6.应用特殊效果修改
            entryStats.zocPower = definition.defaultZocPower + commanderZocBonus;
            entryStats.visionRange = definition.defaultVisionRange + commanderVisionBonus;
            entryStats.moraleConsumption =
                Mathf.FloorToInt(BattleParam.Instance.moraleConsumePreTurn * (1.0f -(Mathf.Min(commanderMoraleConsumptionBonus,30)/100.0f))); //最大不超过30
            
            ExtraMovement = commanderMovementBonus;
            
            //7.初始化移动力
            currentMovement = MaxMovement;
            Debug.Log($"[Stats] Battalion {name} Entry Stats Calculated.");
        }
        
        float CalculateBonus(CommanderStatScaling scaling)
        {
            if (scaling == null) return 0;
            return battalionCommander.GetTotalZhi() * scaling.zhiFactor +
                   battalionCommander.GetTotalXin() * scaling.xinFactor +
                   battalionCommander.GetTotalRen() * scaling.renFactor +
                   battalionCommander.GetTotalYong() * scaling.yongFactor +
                   battalionCommander.GetTotalYan() * scaling.yanFactor;
        }
        
        public void ConsumeMoralePerTurn()
        {
            int consumption = Mathf.RoundToInt(GetStat(StatType.MoraleConsumption));
            
            if (consumption < 0) consumption = 0;

            if (consumption > 0)
            {
                currentMorale -= consumption;
                currentMorale = Mathf.Max(0, currentMorale);
                Debug.Log($"[Morale] {name} consumed {consumption} morale. Current: {currentMorale}");
                
                if (currentMorale <= 0)
                {
                    // HandleMoraleBreak();
                }
            }
        }
        
        

        private void MoraleBreak()
        {
            if (BattleParam.Instance == null) return;
            float rate = BattleParam.Instance.moraleBreakAttritionRate;
            int lostSoldiers = Mathf.FloorToInt(currentSoliders * rate);
            
            if (lostSoldiers == 0 && currentSoliders > 0) lostSoldiers = 1;
            
            if (lostSoldiers > 0)
            {
                currentSoliders -= lostSoldiers;
                currentSoliders = Mathf.Max(0, currentSoliders);
                
                Debug.Log($"[Morale Break] {name} lost {lostSoldiers} soldiers due to low morale. Remaining: {currentSoliders}");
                
                OnHealthChanged(); // 更新 UI

                if (Battle.Instance != null)
                {
                    Battle.Instance.MarkUnitDirty(this); // 检查是否因此死亡
                }
            }
        }
        
        public override void UpdateVisualState()
        {
            base.UpdateVisualState();
            if (visualController is BattalionVisuals batVisuals)
            {
                batVisuals.SetBattalionState(_currentSoldierState);
            }
        }
        
    }
}
