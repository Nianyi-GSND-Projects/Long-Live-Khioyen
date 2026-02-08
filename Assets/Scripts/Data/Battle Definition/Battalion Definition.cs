using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Battalion Definition")]
    public class BattalionDefinition : UnitDefinition
    {
        
        public string[] tags;
        
        #region Battalion Attributes
        public int defaultDiscipline;
        public int defaultPower;
        public int defaultDefence;
        public int defaultFlexibility;
        public int defaultStrategy;
        
        public int defaultMaxSolider;
        public int defaultMaxMorale;
        
        #endregion
        
        [Header("Visuals")]
        public GameObject soldierModelPrefab;
        public int soldiersPerModel = 100;
        public float modelSpacing = 0.05f;
        
        public BattalionAttackType battalionAttackType;
        public int attackRange;

    }
}
