using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Battalion Definition")]
    public class BattalionDefinition : UnitDefinition
    {
        
        public string[] tags;
        
        public bool isTrainable = false;
        
        [Header("Training")]
        public ItemDefinition[] requiredItems = new ItemDefinition[0];
        public string[] requiredBuildingTags = new string[0];
        
        #region Battalion Attributes
        
        public int defaultDiscipline;
        public int defaultFlexibility;
        public int defaultStrategy;
        
        public int defaultMaxSolider;
        public int defaultMaxMorale;
        
        #endregion
        
        [Header("Visuals")]
        public int soldiersPerModel = 100;
        public float modelSpacing = 0.05f;
        [Tooltip("如果 soldierModelPrefab 使用 SpriteRenderer，可以为其指定一个特定的材质。如果为空，则使用默认材质。")] 
        public Material spriteMaterial;
        
        public BattalionAttackType battalionAttackType;
        public int attackRange;
    }
}
