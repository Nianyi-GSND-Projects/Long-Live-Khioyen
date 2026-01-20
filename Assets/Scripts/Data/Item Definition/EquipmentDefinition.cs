using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Equipment Definition")]
    public class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public int equipmentId;
        public string equipmentName;
        public string[] tags;
        public string description;
        
        [Header("Visual")]
        public GameObject equipmentPrefab;
        public Sprite icon;
        
        [Header("Value")]
        public Rarity rarity;
        
        public float value;

        [Header("Stats")] 
        public int advancedRen = 1;
        //TODO 装备实际加成与功能
    }
}
