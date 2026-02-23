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
            if (Battle.Instance == null) return;
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentEvent == null) return;
            
            Vector3 worldPos = Battle.Instance.AnchorPosition;
            Vector2Int gridPos = Battle.Instance.WorldToMapInt(worldPos);
            
            BattleEventManager.Instance.CurrentEvent.SetData(outputKey, gridPos);
            
            Debug.Log($"[GetCameraPos] Saved camera pos {gridPos} to '{outputKey}'");
        }
    }
}