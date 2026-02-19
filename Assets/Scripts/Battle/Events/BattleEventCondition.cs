using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    public enum EventConditionType
    {
        TurnCountEquals,
        TurnCountGreaterThan,
        UnitIsDead, 
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

        public bool Evaluate()
        {
            if (Battle.Instance == null) return false;

            switch (conditionType)
            {
                case EventConditionType.TurnCountEquals:
                    return Battle.Instance.TurnCount == intValue;
                    
                case EventConditionType.TurnCountGreaterThan:
                    return Battle.Instance.TurnCount > intValue;

                case EventConditionType.UnitIsDead:
                    // 目前 Battle.cs 没有直接通过 ID 查找死亡单位的接口
                    // 我们可以暂时返回 false，或者后续在 Battle 中添加 DeadUnits 列表
                    return false; 

                case EventConditionType.FactionUnitCountLessThan:
                    if (Battle.Instance.GetUnitsByFaction(faction) != null)
                    {
                        return Battle.Instance.GetUnitsByFaction(faction).Count < intValue;
                    }
                    return false;
                    
                case EventConditionType.UnitAtPosition:
                    if (Battle.Instance.IsValidMapPosition(vectorValue))
                    {
                        var tile = Battle.Instance.mapData[vectorValue.x, vectorValue.y];
                        // 检查部队
                        if (tile.Battalion != null && tile.Battalion.Definition == unitDefinition)
                            return true;
                        // 检查设施
                        if (tile.Facility != null && tile.Facility.Definition == unitDefinition)
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}