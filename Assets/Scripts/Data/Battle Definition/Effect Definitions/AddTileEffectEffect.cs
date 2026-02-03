using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Game/Effects/Add Tile Effect")]
    public class AddTileEffectEffect : EffectDefinition
    {
        public TileEffectDefinition tileEffectDef;
        public int duration = 3;

        public override void Execute(ActionContext ctx)
        {
            
            Battle.Instance.AddTileEffect(ctx.TargetPos, tileEffectDef, duration, ctx.User);

        }
    }
}