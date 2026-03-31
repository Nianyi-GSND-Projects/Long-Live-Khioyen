using System;
using UnityEngine;

namespace LongLiveKhioyen.Conditions
{
    [Serializable]
    public class Condition_LastPolisConquered : BattleEventCondition
    {
        [Tooltip("期望的城邦占领状态（True=已占领，False=未占领）")]
        public bool expectedState = true;

        public override bool Evaluate(BattleEventContext ctx)
        {
            if (GameInstance.Instance.LastPolis == null)
            {
                Debug.LogWarning("[Condition_LastPolisConquered] GameInstance.LastPolis 为空，条件默认返回 false。");
                return false;
            }

            return GameInstance.Instance.LastPolis.conquered == expectedState;
        }
    }
}