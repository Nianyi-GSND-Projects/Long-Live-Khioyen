using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Place Facility")]
    public class PlaceFacilityEffect : EffectDefinition
    {
        [Header("Visuals")]
        [Tooltip("建立地基时的沙土飞扬特效")]
        public GameObject buildVfxPrefab;

        public override IEnumerator ExecuteCoroutine(ActionContext ctx)
        {
            if (Battle.Instance == null) yield break;
            
            float t = BattleParam.Instance != null ? BattleParam.Instance.actionAnimationDuration : 0.5f;
            float focusDist = BattleParam.Instance != null ? BattleParam.Instance.focusCameraDistance : 6f;
            float camTransitionTime = BattleParam.Instance != null ? BattleParam.Instance.cameraTransitionDuration : 0.15f;

            BattleCameraController camController = null;
            if (Battle.Instance.inputController != null)
                camController = Battle.Instance.inputController.cameraController;
            Battalion builderBat = ctx.User as Battalion;
            
            if (camController != null && ctx.User != null)
            {
                camController.FocusOnPosition(Battle.Instance.MapToWorld(ctx.User.position), focusDist, camTransitionTime);
            }

            if (builderBat != null)
            {
                builderBat.CurrentSoldierState = SoldierState.Cast; // 使用 Cast 状态表现建造动作
            }
            yield return new WaitForSeconds(t);
            
            Vector3 targetWorldPos = Battle.Instance.MapToWorld(ctx.TargetPos);
            
            if (camController != null)
            {
                camController.FocusOnPosition(targetWorldPos, focusDist, camTransitionTime);
            }

            if (buildVfxPrefab != null)
            {
                Instantiate(buildVfxPrefab, targetWorldPos, Quaternion.identity);
            }
            Unit facility = Battle.Instance.BuildPendingFacility(ctx.TargetPos, ctx.User.faction);
            // 等待 t 秒，让沙土飞扬的动画先播放一会
            yield return new WaitForSeconds(t);
            
            if (facility != null)
            {
                Debug.Log($"Built {facility.name} at {ctx.TargetPos}");
            }

            // 停留一会，让玩家看清楚新造好的建筑
            yield return new WaitForSeconds(t);
            
        }
    }
}