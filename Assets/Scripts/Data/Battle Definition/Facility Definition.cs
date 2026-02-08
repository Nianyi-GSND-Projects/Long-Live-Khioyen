using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public struct FacilityState
    {
        [Range(0, 1)] public float healthPercentage; // 低于这个百分比时显示
        public GameObject stateModel; // 对应的模型预制体
    }
    
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Facility Definition")]
    public class FacilityDefinition : UnitDefinition
    {
        public string[] tags;
        
        public int defaultMaxDurability;
        
        public int defaultCost;
        
        public int defaultPower;

        public bool special;

        public bool block = false;
        
        [Header("Visuals")]
        public List<FacilityState> damageStates;
    }
}
