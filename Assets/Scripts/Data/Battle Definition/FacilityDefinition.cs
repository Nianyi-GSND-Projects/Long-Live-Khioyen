using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Facility Definition")]
    public class FacilityDefinition : UnitDefinition
    {
        public string[] tags;
        
        public string description;
        
        public int defaultMaxDurability;
        
        public int defaultCost;

        public bool special;

        public bool block = false;
        
        [Header("Visuals")]
        [Tooltip("是否像部队一样，使用多个模型实例来组成一个单位")]
        public bool useFormationDisplay = false;
        [Tooltip("如果使用阵型显示，生成多少个模型实例")]
        [Min(1)] public int formationInstanceCount = 1;
        [Tooltip("如果使用阵型显示，模型之间的间距")]
        public float formationSpacing = 0.5f;
        [Header("Construction Visuals")]
        [Tooltip("建设过程中的模型列表。会根据建设进度（耐久度百分比）自动选择。")]
        public List<GameObject> constructionStagePrefabs = new List<GameObject>();
        
        [Header("Interaction")]
        public bool isInteractable = false;
        
        public virtual void OnInteract(Unit user, Facility facility)
        {
            Debug.Log($"{user.name} 与 {facility.name} 交互了，但什么也没发生。");
        }
    }
}
