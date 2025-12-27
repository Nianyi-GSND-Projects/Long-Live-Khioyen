using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class ActionContext
    {
        public Unit User;
        public Unit Target;
    }

    public enum TargetFactionType
    {
        Friend,
        NonFriend,
        Enemy,
        All
    }

    public enum TargetCountType
    {
        Self,
        Single,
        Multiple
    }
    
    [CreateAssetMenu(menuName = "Long Live Khioyen/Action Definition")]
    public class ActionDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string actionName;
        public int actionId;
        
        [TextArea] public string description;
        
        public Sprite icon;
        
        [Header("Cost")]
        public int actionPointCost;
        
        public TargetFactionType targetFactionType;
        public TargetCountType targetCountType;
        
        [Header("Logic")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();

        public bool Perform(Unit user, Unit target)
        {
            //支付费用
            //user.TakeActionPoint(actionPointCost);
            
            ActionContext ctx = new ActionContext{User = user, Target = target};
            
            foreach(var effect in effects) effect.Execute(ctx);
            
            Debug.Log($"Action {actionName} performed by {user.InstanceId} on {target.InstanceId}");
            
            return true;
        }
    }
}
