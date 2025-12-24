using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Unit Definition/Battalion Definition")]
    public class BattalionDefinition : UnitDefinition
    {
        
        public string[] tags;
        
        #region Battalion Attributes
        public int defaultDiscipline;
        public int defaultAttack;
        public int defaultDefence;
        public int defaultFlexibility;
        public int defaultStrategy;
        
        public int defaultMaxSolider;
        public int defaultMaxMorale;
        
        #endregion
        
        public BattalionAttackType battalionAttackType;
        public int attackRange;

    }
}
