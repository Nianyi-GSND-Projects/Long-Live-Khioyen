using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Unit Definition/Facility Definition")]
    public class FacilityDefinition : UnitDefinition
    {
        public string[] tags;
        
        public int defaultMaxDurability;

        public bool special;
    }
}
