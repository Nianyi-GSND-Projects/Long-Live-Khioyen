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
    
    public enum UnitPassability { Impassable, Passable, Stoppable,AlliesStoppable,AlliesPassable}

    
    public abstract class UnitDefinition : ScriptableObject
    {
        public string unitName;
        public UnitType unitType;
        
        //有设施和部队两个子类
        #region Asset
        
        public Sprite figure;
        public GameObject unitModelTemplate;
        //public GameObject ModelTemplate => Resources.Load<GameObject>($"Models/Battalions/{armyId}");
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
        
    }
    
    
    
    
}