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

        
        
        //有设施和部队两个子类
        #region Asset
        
        public Sprite icon;
        public AudioClip unitSelectedSoundEffect;
        [Header("Visual Model")]
        [Tooltip("单位的基础模型 Prefab (对于部队是单个士兵，对于设施是完整建筑)")]
        public GameObject unitModelPrefab;
        [Tooltip("是否让模型始终面向摄像机 (Billboard)")]
        public bool useBillboard = false;
        
        #endregion
        
        #region Stats
        [Tooltip("默认攻击力：每100个士兵所能默认提供的攻击力")]
        public int defaultPower;
        public int defaultDefense;
        [Tooltip("默认修补能力：每100个士兵在一次修补中提供的耐久恢复量")]
        public int defaultRepairPower = 10;
        public int defaultZOCPower = 1;
        public bool defaultVisibility = true;
        
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