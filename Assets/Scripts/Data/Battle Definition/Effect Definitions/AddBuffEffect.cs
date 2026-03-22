using System.Collections;
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
        
        [Header("Visuals")]
        public GameObject vfxPrefab;
        [Tooltip("特效生成时的垂直高度偏移，用于确保特效出现在单位头顶")]
        public float vfxHeightOffset = 1.5f; 

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            Unit target = ctx.TargetUnit;
            if (target == null || buffDefinition == null) yield break; // 注意：协程中使用 yield break 替代 return
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;
            
            Battalion casterBat = ctx.User as Battalion;
            
            // ==========================================
            // 动画阶段 1：聚焦施法者，播放 Cast 动作
            // ==========================================
            if (camController != null && ctx.User != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(ctx.User.position), focusDist, camTransitionTime);
            }
            if (casterBat != null) casterBat.CurrentSoldierState = SoldierState.Cast; 
            
            yield return new WaitForSeconds(t);

            // ==========================================
            // 动画阶段 2：聚焦目标，播放特效并施加 Buff
            // ==========================================
            if (camController != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(target.position), focusDist, camTransitionTime);
            }

            if (vfxPrefab != null)
            {
                Vector3 spawnPos = target.transform.position + Vector3.up * vfxHeightOffset;
                Instantiate(vfxPrefab, spawnPos, Quaternion.identity);
            }

            BuffDescriptor desc = new BuffDescriptor
            {
                definition = buffDefinition,
                defaultDuration = duration
            };
            target.ApplyBuff(desc);

            yield return new WaitForSeconds(t);
        }
    }
}