using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Add Tile Effect")]
    public class AddTileEffectEffect : EffectDefinition
    {
        [Header("Effect Settings")] 
        public TileEffectDefinition tileEffectDef;
        public int duration = 3;
        
        [Header("Probability Settings")]
        [Tooltip("是否启用概率判定？如果不勾选，则必定成功。")]
        public bool useProbability = false;
        
        [Tooltip("基础成功率 (0-100)")]
        [Range(0, 100)]
        public int baseProbability = 50;

        public override void Execute(ActionContext ctx)
        {
            if (useProbability)
            {
                int roll = Random.Range(0, 100);

                if (roll >= baseProbability)
                {
                    Debug.Log($"[Effect] 地块效果施加判定失败 (Roll: {roll} >= {baseProbability})");
                    return;
                }
                
                Debug.Log($"[Effect] 地块效果判定成功！(Roll: {roll})");
            }
            
            if (Battle.Instance != null && tileEffectDef != null)
            {
                Battle.Instance.AddTileEffect(ctx.TargetPos, tileEffectDef, duration, ctx.User);
            }

        }
    }
}