using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    public enum EventConditionType
    {
        TurnCountEquals,
        TurnCountGreaterThan,
        GlobalFlagIsTrue,
        
        ContextUnitIsFaction,
        ContextUnitIsID,
        ContextUnitIsDefinition,
        UnitIsAlive,
        
        UnitAtPosition, 
        FactionUnitCountLessThan, 
        
        Manual 
    }

    [Serializable]
    public class BattleEventCondition
    {
        public EventConditionType conditionType;
        
        [Header("Parameters")]
        public int intValue; 
        public string stringValue; 
        public Vector2Int vectorValue; 
        public UnitDefinition unitDefinition; 
        public Faction faction; 

        public bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;

            switch (conditionType)
            {
                // --- 回合 ---
                case EventConditionType.TurnCountEquals:
                    return Battle.Instance.TurnCount == intValue;
                    
                case EventConditionType.TurnCountGreaterThan:
                    return Battle.Instance.TurnCount > intValue;

                // --- 全局 Flag ---
                case EventConditionType.GlobalFlagIsTrue:
                    if (BattleEventManager.Instance != null)
                    {
                        return BattleEventManager.Instance.GetGlobalData<bool>(stringValue);
                    }
                    return false;

                // --- 上下文单位检查 ---
                case EventConditionType.ContextUnitIsFaction:
                    return ctx.TriggerUnit != null && ctx.TriggerUnit.faction == faction;

                case EventConditionType.ContextUnitIsID:
                    return ctx.TriggerUnit != null && ctx.TriggerUnit.InstanceId == intValue;

                case EventConditionType.ContextUnitIsDefinition:
                    return ctx.TriggerUnit != null && ctx.TriggerUnit.unitDefinition == unitDefinition;

                // --- 场上状态 ---
                case EventConditionType.FactionUnitCountLessThan:
                    var units = Battle.Instance.GetUnitsByFaction(faction);
                    return units != null && units.Count < intValue;
                
                case EventConditionType.UnitIsAlive:
                    // 检查特定 ID 的单位是否在场且活着
                    var unit = Battle.Instance.GetUnitByInstanceId(intValue);
                    return unit != null && unit.gameObject.activeSelf && unit.currentHealth > 0;

                    
                case EventConditionType.UnitAtPosition:
                    if (Battle.Instance.IsValidMapPosition(vectorValue))
                    {
                        var tile = Battle.Instance.mapData[vectorValue.x, vectorValue.y];
                        if (tile.Battalion != null && tile.Battalion.Definition == unitDefinition) return true;
                        if (tile.Facility != null && tile.Facility.Definition == unitDefinition) return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}