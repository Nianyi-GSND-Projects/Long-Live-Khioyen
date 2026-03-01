using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class LootDropRule
    {
        public LootTableSO lootTable;
        [Range(0, 100)] public int dropChance = 100; // 触发该表的概率
    }
    
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

        public bool defaultVisibility = true;
        
        //有设施和部队两个子类
        #region Asset
        
        public Sprite figure;
        public GameObject unitModelTemplate;
        public AudioClip unitSelectedSoundEffect;
        
        #endregion
        
        #region Stats
        
        public int defaultPower;
        public int defaultDefense;
        
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
        public ActionDefinition defaultConstructAction;
        public ActionDefinition defaultRepairAction;
        
        public List<ActionDefinition> unitUniqueActions = new List<ActionDefinition>();
        
        #endregion

        #region Loot

        [Header("Loot")]
        public List<LootDropRule> lootRules = new List<LootDropRule>();

        #endregion
    }
    
    
    
    
}