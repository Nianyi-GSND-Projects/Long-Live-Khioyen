using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Facility : Unit<FacilityDefinition>
    {
        public int currentDurability;
        
        public override void TakeDamage(int damage)
        {
            currentDurability -= damage;
            if (Battle.Instance != null) 
                Battle.Instance.MarkUnitDirty(this);
        }
        
        public override float GetPower()
        {
            return Definition.defaultPower;
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
    }
    
    public class FacilityDescriptor
    {
        public int InstanceId;
        public Vector2Int position;
        public FacilityDefinition Definition;
        public int currentDurability;
    }
}
