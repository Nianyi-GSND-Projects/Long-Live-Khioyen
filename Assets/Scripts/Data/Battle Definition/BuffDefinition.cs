using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace LongLiveKhioyen
{
    public enum BuffFactionType
    {
        Friend,
        Enemy,
        Self,
        All
    }

    public enum BuffUnitType
    {
        Battalion,
        Facility,
        Both
    }
    [Serializable]
    public class BuffDescriptor
    {
        [SerializeReference]
        public BuffDefinition definition;
        public int defaultDuration;
    }

    public class Buff
    {
        public BuffDescriptor descriptor;
        public int currentDuration;
        
        public void TimePass()
        {
            currentDuration--;
        }
    }
    [Serializable]
    public abstract class BuffDefinition: ScriptableObject
    {
        public string buffNameEn;
        public string buffNameCh;
        public Sprite icon;
        
        public BuffFactionType factionType;
        
        public BuffUnitType unitType;
        
        public GameObject vfxPrefab; 
        
        public virtual void OnApply(Unit unit, Buff runtimeBuff) { }
        
        public virtual void OnTick(Unit unit, Buff runtimeBuff) { }
        
        public virtual void OnRemove(Unit unit, Buff runtimeBuff) { }
    }
   
}