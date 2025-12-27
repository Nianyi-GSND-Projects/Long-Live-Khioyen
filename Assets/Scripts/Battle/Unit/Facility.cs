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
        }
        
        public override float GetPower()
        {
            return Definition.defaultPower;
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
