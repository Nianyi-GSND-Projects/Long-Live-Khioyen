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

        public bool special;

        public bool block = false;
        
        [Header("Visuals")]
        public List<FacilityState> damageStates;
        
        [Header("Interaction")]
        public bool isInteractable = false;
        
        public virtual void OnInteract(Unit user, Facility facility)
        {
            Debug.Log($"{user.name} 与 {facility.name} 交互了，但什么也没发生。");
        }
    }
}
