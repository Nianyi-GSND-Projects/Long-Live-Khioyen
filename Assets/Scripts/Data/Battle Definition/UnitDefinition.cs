using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    
    public enum BattalionAttackType
    {
        Melee,
        Ranged,
        NonBattle
    }
    
    public enum UnitType { Battalion, Facility }
    
    public abstract class UnitDefinition : ScriptableObject
    {
        [Header("Database Info")]
        public int id;
        public string unitName;
        public UnitType unitType;
        
        //有设施和部队两个子类
        #region Asset
        
        public Sprite figure;
        public GameObject unitModelTemplate;
        public AudioClip unitSelectedSoundEffect;
        
        #endregion
        
        
        #region Accessibility
        
        public UnitPassability passability;
        
        public bool movable;
        public bool actionable;
        
        public bool beAttacked;
        public bool beInteracted;
        public bool beMoved;

        #endregion
        
        #region Action
        
        [Header("Actions")]
        [Tooltip("单位的基础攻击方式 (例如：普通攻击)")]
        public ActionDefinition defaultAttack;
        public ActionDefinition defaultRetreat;
        public ActionDefinition defaultInteract;
        
        public List<ActionDefinition> unitUniqueActions = new List<ActionDefinition>();
        
        #endregion
    }
    
    
    
    
}