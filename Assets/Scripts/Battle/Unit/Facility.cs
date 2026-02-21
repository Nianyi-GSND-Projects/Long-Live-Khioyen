using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Facility : Unit<FacilityDefinition>
    {
        public int currentDurability
        {
            get => currentHealth;
            set => currentHealth = value;
        }
        
        public override float GetPower()
        {
            if (entryStats == null) return 0;
            float ratio = 1.0f;
            return entryStats.attackPower * ratio; 
        }
        
        public override float GetDefense()
        {
            if (entryStats == null) return 0;
            float ratio = 1.0f;
            return entryStats.defensePower* ratio;
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


            // 2. 科技修正 (假设有)
            // float techMod = TechManager.Instance.GetBuildingHealthBonus();

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
