using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Get Camera Position")]
    public class GetCameraPositionAction : GameEventAction
    {
        [Header("Output")]
        public string outputKey = "CameraPos";
        
        public override void Execute()
        {
            Debug.LogWarning($"[GetCameraPositionAction] 执行失败：缺少 BattleEventContext，无法将摄像机位置保存到 '{outputKey}'。");
        }
        
        public override void Execute(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return;
            
            if (ctx == null)
            {
                Execute();
                return;
            }
            
            Vector3 worldPos = Battle.Instance.AnchorPosition;
            Vector2Int gridPos = Battle.Instance.WorldToMapInt(worldPos);
            
            ctx.SetData(outputKey, gridPos);
            
            Debug.Log($"[GetCameraPos] Saved camera pos {gridPos} to '{outputKey}' in Context");
        }
    }
}