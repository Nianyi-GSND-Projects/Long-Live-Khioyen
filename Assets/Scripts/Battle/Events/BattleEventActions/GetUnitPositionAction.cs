using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Get Unit Position")]
    public class GetUnitPositionAction : GameEventAction
    {
        [Header("Input Source")]
        public bool useBlackboardID = false; // [新增] 开关
        public string inputIDKey = "UnitID"; // [新增] 读取 ID 的 Key

        [Header("Static Input")]
        public int targetUnitInstanceId;

        [Header("Output")]
        public string outputKey = "TargetPos";

        public override void Execute()
        {
            if (Battle.Instance == null) return;
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentEvent == null) return;

            int finalID = targetUnitInstanceId;

            // [新增] 从黑板读取 ID
            if (useBlackboardID)
            {
                var evt = BattleEventManager.Instance.CurrentEvent;
                if (evt.HasData(inputIDKey))
                {
                    finalID = evt.GetData<int>(inputIDKey);
                    Debug.Log($"[GetUnitPosition] Read ID {finalID} from key '{inputIDKey}'");
                }
                else
                {
                    Debug.LogWarning($"[GetUnitPosition] Key '{inputIDKey}' not found. Using static ID {finalID}");
                }
            }

            Debug.Log($"[GetUnitPosition] Searching for Unit ID: {finalID}...");

            Unit unit = Battle.Instance.GetUnitByInstanceId(finalID);
            
            if (unit != null)
            {   
                BattleEventManager.Instance.CurrentEvent.SetData(outputKey, unit.position);
                Debug.Log($"[GetUnitPosition] SUCCESS: Found Unit '{unit.name}' at {unit.position}. Saved to '{outputKey}'.");
            }
            else
            {
                Debug.LogWarning($"[GetUnitPosition] FAILED: Unit {finalID} not found!");
            }
        }
    }
}