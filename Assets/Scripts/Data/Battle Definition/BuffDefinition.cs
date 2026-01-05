using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public class BuffDescriptor
    {
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
    
    public abstract class BuffDefinition : ScriptableObject
    {
        public BuffFactionType factionType;
        
        public BuffUnitType unitType;
    }
   
}