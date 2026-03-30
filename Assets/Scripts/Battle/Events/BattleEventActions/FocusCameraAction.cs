using System.Collections;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Focus Camera")]
    public class FocusCameraAction : GameEventAction
    {
        [Header("Source")]
        public bool useBlackboard = false;
        public string inputKey = "TargetPos";

        [Header("Fallback / Static Target")]
        public Vector2Int targetCoordinates;

        [Header("Camera Settings")]
        public float duration = 1.0f;

        // ==========================================
        // 1. 无参版本 (兼容旧接口，直接启动协程)
        // ==========================================
        public override void Execute()
        {
            if (Battle.Instance != null)
            {
                // 旧接口调用时不带 Context
                Battle.Instance.StartCoroutine(ExecuteCoroutine(null));
            }
        }

        public override IEnumerator ExecuteCoroutine()
        {
            yield return ExecuteCoroutine(null);
        }

        // ==========================================
        // 2. 战斗专用版本 (带 Context 的核心逻辑)
        // ==========================================
        public override void Execute(BattleEventContext ctx)
        {
            if (Battle.Instance != null)
            {
                Battle.Instance.StartCoroutine(ExecuteCoroutine(ctx));
            }
        }

        public override IEnumerator ExecuteCoroutine(BattleEventContext ctx)
        {
            if (Battle.Instance == null) yield break;

            Vector2Int finalCoords = targetCoordinates;

            // 逻辑变更：直接从传入的 ctx 中读取黑板数据
            if (useBlackboard && ctx != null)
            {
                if (ctx.HasData(inputKey))
                {
                    finalCoords = ctx.GetData<Vector2Int>(inputKey);
                }
                else
                {
                    Debug.LogWarning($"[FocusCamera] Key '{inputKey}' NOT found in Context. Using static coords: {targetCoordinates}");
                }
            }

            if (Battle.Instance.IsValidMapPosition(finalCoords))
            {
                Vector3 targetWorldPos = Battle.Instance.MapToWorld(finalCoords);
                
                // 执行相机移动
                Battle.Instance.FocusCamera(targetWorldPos, duration);
                
                // 如果是阻塞模式或需要等待相机到达
                if (duration > 0)
                {
                    yield return new WaitForSeconds(duration);
                }
            }
            else
            {
                Debug.LogWarning($"FocusCameraAction: Invalid coordinates {finalCoords}");
            }
        }
    }
}