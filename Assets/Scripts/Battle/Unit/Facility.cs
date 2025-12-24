using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Facility : Unit<FacilityDefinition>
    {
        public int currentDurability;
    }
    
    public class FacilityDescriptor
    {
        public int InstanceId;
        public Vector2Int position;
        public FacilityDefinition Definition;
        public int currentDurability;
    }
}
