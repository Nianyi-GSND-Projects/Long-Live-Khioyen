using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Get Unit Position")]
    public class GetUnitPositionAction : GameEventAction
    {
        [Header("Input Source")]
        public bool useBlackboardID = false; 
        public string inputIDKey = "UnitID"; 

        [Header("Static Input")]
        public int targetUnitInstanceId;

        [Header("Output")]
        public string outputKey = "TargetPos";

        // ==========================================
        // 1. 无参版本 (警告：缺少上下文无法保存输出)
        // ==========================================
        public override void Execute()
        {
            Debug.LogWarning($"[GetUnitPositionAction] 执行失败：缺少 BattleEventContext，无法读取 ID 或将坐标保存到 '{outputKey}'。");
        }

        // ==========================================
        // 2. 战斗专用版本 (读写 Context 数据)
        // ==========================================
        public override void Execute(BattleEventContext ctx)
        {
            if (Battle.Instance == null) return;
            
            // 如果上下文为空，回退到无参版本打印警告
            if (ctx == null)
            {
                Execute();
                return;
            }

            int finalID = targetUnitInstanceId;

            // [修改] 从传入的 Context 读取 ID
            if (useBlackboardID)
            {
                if (ctx.HasData(inputIDKey))
                {
                    finalID = ctx.GetData<int>(inputIDKey);
                    Debug.Log($"[GetUnitPosition] Read ID {finalID} from key '{inputIDKey}' in Context");
                }
                else
                {
                    Debug.LogWarning($"[GetUnitPosition] Key '{inputIDKey}' not found in Context. Using static ID {finalID}");
                }
            }

            Debug.Log($"[GetUnitPosition] Searching for Unit ID: {finalID}...");

            Unit unit = Battle.Instance.GetUnitByInstanceId(finalID);
            
            if (unit != null)
            {   
                // [修改] 将坐标保存到当前事件独有的临时黑板中
                ctx.SetData(outputKey, unit.position);
                Debug.Log($"[GetUnitPosition] SUCCESS: Found Unit '{unit.name}' at {unit.position}. Saved to '{outputKey}' in Context.");
            }
            else
            {
                Debug.LogWarning($"[GetUnitPosition] FAILED: Unit {finalID} not found!");
            }
        }
    }
}