using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Equipment Definition")]
    public class EquipmentDefinition : ItemDefinition
    {
        [Header("Identity")]
        
        [Header("Visual")]

        [Header("Stats")] 
        public int advancedRen = 1;
        //TODO 装备实际加成与功能
    }
}
