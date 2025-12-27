using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
   
    public enum EffectType
    {
        Nothing,
        Attack,
        Buff,
        Debuff,
        Interact,
        GetResource,
        ForceToMove,
        Build,
        Other
    }
    
    
    public abstract class EffectDefinition : ScriptableObject
    {
        public EffectType effectType;
        public abstract void Execute(ActionContext context);
    }
    
   
}
