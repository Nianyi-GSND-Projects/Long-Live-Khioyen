using System;
using UnityEngine;

namespace LongLiveKhioyen.Conditions
{
    [Serializable]
    public class Condition_FactionUnitCountLessThan : BattleEventCondition
    {
        public Faction targetFaction;
        [Tooltip("该阵营的存活单位数量必须少于此值")]
        public int countThreshold;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;
            
            var units = Battle.Instance.GetUnitsByFaction(targetFaction);
            return units != null && units.Count < countThreshold;
        }
    }

    [Serializable]
    public class Condition_UnitIsAlive : BattleEventCondition
    {
        [Tooltip("要检查存活状态的单位 Instance ID")]
        public int targetInstanceId;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;

            // 检查特定 ID 的单位是否在场且活着
            var unit = Battle.Instance.GetUnitByInstanceId(targetInstanceId);
            return unit != null && unit.gameObject.activeSelf && unit.currentHealth > 0;
        }
    }

    [Serializable]
    public class Condition_UnitAtPosition : BattleEventCondition
    {
        [Tooltip("要检查的网格坐标")]
        public Vector2Int targetPosition;
        
        [Tooltip("该位置上必须是这种单位。如果为空，则只要有人就算满足。")]
        public UnitDefinition requiredDefinition;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return false;

            if (Battle.Instance.IsValidMapPosition(targetPosition))
            {
                var tile = Battle.Instance.mapData[targetPosition.x, targetPosition.y];
                
                // 检查战斗单位 (Battalion)
                if (tile.Battalion != null)
                {
                    if (requiredDefinition == null) return true; // 不限种类，有人就行
                    if (tile.Battalion.Definition == requiredDefinition) return true;
                }
                
                // 检查设施建筑 (Facility)
                if (tile.Facility != null)
                {
                    if (requiredDefinition == null) return true;
                    if (tile.Facility.Definition == requiredDefinition) return true;
                }
            }
            return false;
        }
    }
}