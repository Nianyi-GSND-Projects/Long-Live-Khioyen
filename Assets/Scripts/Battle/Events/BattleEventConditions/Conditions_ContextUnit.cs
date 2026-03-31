using System;
using UnityEngine;

namespace LongLiveKhioyen.Conditions
{
    [Serializable]
    public class Condition_ContextUnitIsFaction : BattleEventCondition
    {
        [Tooltip("触发单位必须属于该阵营")]
        public Faction requiredFaction;

        public override bool Evaluate(BattleEventContext ctx)
        {
            return ctx != null && ctx.TriggerUnit != null && ctx.TriggerUnit.faction == requiredFaction;
        }
    }

    [Serializable]
    public class Condition_ContextUnitIsID : BattleEventCondition
    {
        [Tooltip("触发单位的 Instance ID 必须匹配")]
        public int requiredInstanceId;

        public override bool Evaluate(BattleEventContext ctx)
        {
            return ctx != null && ctx.TriggerUnit != null && ctx.TriggerUnit.InstanceId == requiredInstanceId;
        }
    }

    [Serializable]
    public class Condition_ContextUnitIsDefinition : BattleEventCondition
    {
        [Tooltip("触发单位的种类 (Definition) 必须匹配")]
        public UnitDefinition requiredDefinition;

        public override bool Evaluate(BattleEventContext ctx)
        {
            return ctx != null && ctx.TriggerUnit != null && ctx.TriggerUnit.unitDefinition == requiredDefinition;
        }
    }
}