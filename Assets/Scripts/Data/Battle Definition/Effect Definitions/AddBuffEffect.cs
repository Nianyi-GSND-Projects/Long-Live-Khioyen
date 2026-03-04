using System.Collections.Generic;
using UnityEngine;
using System;
namespace LongLiveKhioyen
{
    [Serializable]
    public class AddBuffEffect : EffectDefinition
    {
        public BuffDefinition buffDefinition;
        public int duration;
        public GameObject vfxPrefab;

        public override void Execute(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            if (target == null || buffDefinition == null) return;

            BuffDescriptor desc = new BuffDescriptor
            {
                definition = buffDefinition,
                defaultDuration = duration
            };
            target.ApplyBuff(desc);
        }
    }
}