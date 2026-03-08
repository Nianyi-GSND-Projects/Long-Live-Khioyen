using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LongLiveKhioyen
{
    public class ActionContext
    {
        public Unit User;
        public Vector2Int TargetPos;
        public ActionDefinition ActionDef;
        
        public Unit OriginalTargetUnit; 
        
        public Unit TargetUnit 
        {
            get
            {
                if (Battle.Instance == null) return null;
                TileData tile = Battle.Instance.mapData[TargetPos.x, TargetPos.y];
                
                if (ActionDef != null)
                {
                    if (ActionDef.targetType == ActionTargetType.BattalionOnly) return tile.Battalion;
                    if (ActionDef.targetType == ActionTargetType.FacilityOnly) return tile.Facility;
                    
                    return ActionDef.GetPrimaryTargetOnTile(tile);
                }
                
                return tile.Battalion != null ? tile.Battalion : tile.Facility;
            }
        }
        
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();
        
        public void SetData(string key, object value)
        {
            if (_blackboard.ContainsKey(key)) _blackboard[key] = value;
            else _blackboard.Add(key, value);
        }

        public T GetData<T>(string key)
        {
            if (_blackboard.TryGetValue(key, out object val))
            {
                return (T)val;
            }
            return default(T);
        }
    }

    public enum TargetFactionType
    {
        Friend,
        NonFriend,
        Enemy,
        All
    }
    
    public enum ActionTargetType
    {
        BattalionOnly,  
        FacilityOnly,   
        EmptyTileOnly,  
        UnitOnly, 
        Any 
    }

    public enum TargetCountType
    {
        Self,
        Single,
        Multiple
    }
    
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Action Definition")]
    public class ActionDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string actionName;
        public int actionId;
        
        [TextArea] public string description;
        public Sprite icon;
        
        [Header("Cost")]
        public int actionPointCost;
        
        [Header("Target Requirements")]
        public bool requireInteractable = false;
        public bool requireAttackable = false;
        public TargetFactionType targetFactionType;
        public TargetCountType targetCountType;
        
        [Header("Conditions")]
        [Tooltip("展示条件：通常用于判断是否在UI上显示该技能（例如：只有勇>80才能看到此技能）")]
        public List<ActionCondition> displayConditions = new List<ActionCondition>();
        [Tooltip("使用条件：通常用于判断技能是否可用（例如：士气>10, AP>2）")]
        public List<ActionCondition> useConditions = new List<ActionCondition>();
        
        [Header("Restrictions")]
        [Tooltip("如果为真，该技能在整场战斗中只能使用一次")]
        public bool oncePerBattle = false;

        [Tooltip("如果为真，目标格子必须在施法者的视野内")]
        public bool requireVision = true;
        
        [SerializeReference]
        [Header("Logic")]
        public List<EffectDefinition> effects = new List<EffectDefinition>();
        
        [Header("Constraints")]
        public int maxRange = 1; 
        public int minRange = 1;
        
        [Header("Targeting")]
        public ActionTargetType targetType = ActionTargetType.UnitOnly;
        public bool IsTileValidTarget(Unit user, Vector2Int targetPos)
        {
            return IsTileValidTarget(user, targetPos, user.position);
        }
        
        public bool IsTileValidTarget(Unit user, Vector2Int targetPos,Vector2Int sourcePos)
        {
            if (Battle.Instance == null) return false;
            if (!Battle.Instance.IsValidMapPosition(targetPos))
            {
                return false;
            }
            
            int dist = Battle.Instance.GetHexDistance(sourcePos, targetPos);
            if (dist > maxRange || dist < minRange)
            {
                return false;
            }
            
            if (requireVision)
            {
                if (Battle.Instance != null)
                {
                    if (Battle.Instance.IsTileVisible(targetPos) != FogState.Visible)
                    {
                        return false;
                    }
                }
            }
            TileData tile = Battle.Instance.mapData[targetPos.x, targetPos.y];
            
            Unit primaryTarget = GetPrimaryTargetOnTile(tile);
            if (primaryTarget != null && !Battle.Instance.IsUnitVisibleToPlayer(primaryTarget))
            {
                primaryTarget = null;
            }
            if (requireInteractable)
            {
                bool isInteractable = false;
                if (primaryTarget is Facility fac)
                {
                    isInteractable = fac.Definition.isInteractable;
                }
                else if (primaryTarget is Battalion bat)
                {
                    return false;
                }

                if (!isInteractable) return false;
            }
            
            if (requireAttackable)
            {
                bool isAttackable = false;
                if (primaryTarget.unitDefinition != null)
                {
                    isAttackable = primaryTarget.unitDefinition.beAttacked;
                }
                if (!isAttackable) return false;
            }
            
            switch (targetType)
            {
                case ActionTargetType.BattalionOnly:
                    // 必须有部队，且条件满足
                    return tile.Battalion != null && CheckUnitConditions(user, tile.Battalion);

                case ActionTargetType.FacilityOnly:
                    // 必须有设施，且条件满足
                    return tile.Facility != null && CheckUnitConditions(user, tile.Facility);

                case ActionTargetType.EmptyTileOnly:
                    // 必须完全为空
                    return tile.IsVisualEmpty();

                case ActionTargetType.UnitOnly:
                    // 如果首选目标存在，且满足条件 -> OK
                    if (primaryTarget != null) return CheckUnitConditions(user, primaryTarget);
                    return false;

                case ActionTargetType.Any:
                    // 如果有目标，检查目标；如果是空地，直接通过
                    if (primaryTarget != null) return CheckUnitConditions(user, primaryTarget);
                    return true; // 空地也是合法的
            }
            return false;
        }
        
        public Unit GetPrimaryTargetOnTile(TileData tile)
        {
            bool battalionVisible = Battle.Instance.IsUnitVisibleToPlayer(tile.Battalion);
            bool facilityVisible = Battle.Instance.IsUnitVisibleToPlayer(tile.Facility);
            //都没有/不可见 -> 空地
            if ((tile.Battalion == null || !battalionVisible) && (!facilityVisible || tile.Facility == null))
                return null;
            
            // 只有部队且可见 -> 部队
            if (tile.Battalion != null && tile.Facility == null&&battalionVisible) return tile.Battalion;
            
            // 只有设施且可见 -> 设施
            if (tile.Battalion == null && tile.Facility != null&&facilityVisible) return tile.Facility;

            
            // 情况 C: 都有 -> 看 Block
            if (tile.Battalion != null && tile.Facility != null)
            {
                if (battalionVisible &&
                    !facilityVisible) return tile.Battalion;
                if(!battalionVisible&&
                   facilityVisible) 
                    return tile.Facility;
                
                // 如果设施 Block 为真，设施优先（挡住了部队）
                if (tile.Facility.Definition.block) return tile.Facility;
                
                // 否则部队优先
                return tile.Battalion;
            }

            return null; // 空地
        }
        
        public bool Perform(Unit user, Vector2Int targetPos)
        {
            // 构造 Context
            Unit initialTarget = null;
            
            if (Battle.Instance != null)
            {
                var tile = Battle.Instance.mapData[targetPos.x, targetPos.y];
                initialTarget = GetPrimaryTargetOnTile(tile);
            }
            
            ActionContext ctx = new ActionContext { User = user, TargetPos = targetPos, ActionDef = this, OriginalTargetUnit = initialTarget};
            
            foreach (var effect in effects) 
                effect.Execute(ctx);

            Debug.Log($"Action {actionName} performed at {targetPos}");
            if (oncePerBattle)
            {
                user.MarkActionAsUsed(this);
            }
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
            if ((useConditions == null || useConditions.Count == 0)&&!oncePerBattle)
                return true;
            
            if (oncePerBattle)
            {
                if (user.HasUsedAction(this))
                {
                    return false;
                }
            }

            if ((useConditions == null || useConditions.Count == 0)) return true;
            
            foreach (var condition in useConditions)
            {
                if (!condition.Evaluate(user,null)) 
                    return false;
            }
            return true;
        }
        
        private bool CheckUnitConditions(Unit user, Unit target)
        {
            if (!CheckFactionLogic(user, target)) return false;
            if(target.faction!=Faction.Player&&target.faction!=Faction.Friend)
                if (!target.IsVisible)
                    return false;
            if (useConditions != null)
            {
                foreach (var condition in useConditions)
                {
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
        
        public bool HasValidTargetsInRange(Unit user)
        {
            if (Battle.Instance == null) return false;
            
            return Battle.Instance.HasAnyValidTarget(user, this);
        }
    }
}
