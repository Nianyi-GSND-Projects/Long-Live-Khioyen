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
        
        [Header("Conditions")]
        [Tooltip("展示条件：通常用于判断是否在UI上显示该技能（例如：只有勇>80才能看到此技能）")]
        public List<ActionCondition> displayConditions = new List<ActionCondition>();

        [Tooltip("使用条件：通常用于判断技能是否可用（例如：士气>10, AP>2）")]
        public List<ActionCondition> useConditions = new List<ActionCondition>();
        
        [Header("Logic")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();
        
        [Header("Constraints")]
        public int range = 1; 
        public int minRange = 1;
        public bool CanTargetEmptyTile = false;
        public bool Perform(Unit user, Unit target)
        {
            //支付费用
            //user.TakeActionPoint(actionPointCost);
            
            ActionContext ctx = new ActionContext{User = user, Target = target};
            
            foreach(var effect in effects) effect.Execute(ctx);
            
            Debug.Log($"Action {actionName} performed by {user.InstanceId} on {target.InstanceId}");
            
            return true;
        }

        public bool CheckDisplayConditions(Unit user)
        {
            if (displayConditions == null || displayConditions.Count == 0) return true;

            foreach (var condition in displayConditions)
            {
                if (!condition.Evaluate(user, null)) 
                    return false;
            }
            return true;
        }
        
        public bool CheckUseConditions(Unit user)
        {
            if (useConditions == null || useConditions.Count == 0) return true;

            foreach (var condition in useConditions)
            {
                if (!condition.Evaluate(user,null)) 
                    return false;
            }
            return true;
        }
        
        public bool CheckTargetConditions(Unit user, Unit target)
        {
            // 1. 检查阵营是否匹配
            if (!CheckFactionLogic(user, target)) return false;

            // 2. 检查脚本化条件 (比如：目标必须是受损的才能治疗)
            if (useConditions != null)
            {
                foreach (var condition in useConditions)
                {
                    // 这里传入 target，让 ConditionOperand.GetValue 能够获取目标属性
                    if (!condition.Evaluate(user, target))
                        return false;
                }
            }
            return true;
        }
        
        private bool CheckFactionLogic(Unit user, Unit target)
        {
            switch (targetFactionType)
            {
                case TargetFactionType.Friend:
                    return user.faction == target.faction;
                case TargetFactionType.Enemy:
                    return user.faction != target.faction; // 简单判断，如果有中立阵营需细化
                case TargetFactionType.All:
                    return true;
                default:
                    return false;
            }
        }
    }
}
