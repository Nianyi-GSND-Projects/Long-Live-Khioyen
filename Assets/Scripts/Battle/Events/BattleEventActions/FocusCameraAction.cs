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

        // 兼容旧接口
        public override void Execute()
        {
            // 如果是非阻塞调用，我们启动一个协程但不等待它
            // 注意：ScriptableObject 不能直接 StartCoroutine，需要借助 Battle.Instance
            if (Battle.Instance != null)
            {
                Battle.Instance.StartCoroutine(ExecuteCoroutine());
            }
        }

        // 核心逻辑移入协程
        public override IEnumerator ExecuteCoroutine()
        {
            if (Battle.Instance == null) yield break;

            Vector2Int finalCoords = targetCoordinates;

            if (useBlackboard)
            {
                var evt = BattleEventManager.Instance?.CurrentEvent;
                if (evt != null)
                {
                    if (evt.HasData(inputKey))
                    {
                        finalCoords = evt.GetData<Vector2Int>(inputKey);
                    }
                    else
                    {
                        Debug.LogWarning($"[FocusCamera] Key '{inputKey}' NOT found in blackboard. Using static coords.");
                    }
                }
            }

            if (Battle.Instance.IsValidMapPosition(finalCoords))
            {
                Vector3 targetWorldPos = Battle.Instance.MapToWorld(finalCoords);
                
                // 执行移动
                Battle.Instance.FocusCamera(targetWorldPos, duration);
                
                // 如果是阻塞模式，等待移动完成
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