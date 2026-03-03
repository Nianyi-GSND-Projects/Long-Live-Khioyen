using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class EquipmentDefinition : ItemDefinition
    {
        [Header("Identity")]
        
        [Header("Visual")]
        
        [Header("装备效果")]
        [SerializeReference]
        public List<EquipmentEffect> effects = new List<EquipmentEffect>();
    }
}
