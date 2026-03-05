using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Facility : Unit<FacilityDefinition>
    {
        public bool isConstructed = true;
        public int currentDurability
        {
            get => currentHealth;
            set => currentHealth = value;
        }
        
        public override float GetPower()
        {
            return GetStat(StatType.AttackPower);
        }
        
        public override float GetDefense()
        {
            return GetStat(StatType.DefensePower);
        }

        public override float GetRepairPower()
        {
            return GetStat(StatType.RepairPower);
        }

        protected override void OnHealthChanged()
        {
            base.OnHealthChanged();
        
            if (!isConstructed)
            {
                int max = Definition.defaultMaxDurability;
            
                if (currentDurability >= max)
                {
                    isConstructed = true;
                    currentDurability = max;
                    Debug.Log($"Facility {name} construction complete!");
                    // TODO: 播放完成特效
                }
            }
            OnUnitStateChanged();
        }
        
        public override void OnTurnStart()
        {
            if (!isConstructed) return; // 未建成不回体/不重置行动
            base.OnTurnStart();
        }
        
        public int CurrentCost
        {
            get
            {
                if (entryStats == null) return 0;
                
                float ratio = 1.0f;
                return Mathf.FloorToInt(entryStats.cost * ratio);
            }
        }
        
        public override void ApplyBuff(BuffDescriptor buffDescriptor)
        {
            if (buffDescriptor.definition.unitType == BuffUnitType.Facility ||
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
        
        public override void CalculateEntryStats(UnitDescriptor desc)
        {
            base.CalculateEntryStats(desc);
        
            if (desc is not FacilityDescriptor facDesc) return;
            float baseDef = Definition.defaultDefense;
            float basePower = Definition.defaultPower;
            int rawCost = facDesc.cost > 0 ? facDesc.cost : Definition.defaultCost; // 假设 FacilityDefinition 有 defaultCost
            
            // 3. 赋值
            entryStats.defensePower = baseDef;
            entryStats.maxHealth = facDesc.maxDurability;
            entryStats.attackPower = basePower;
            
            entryStats.cost = rawCost;
            
            currentHealth = facDesc.currentDurability;
            
            Debug.Log($"[Stats] Facility {name} Entry Stats Calculated.");
        }
    }
    
}
