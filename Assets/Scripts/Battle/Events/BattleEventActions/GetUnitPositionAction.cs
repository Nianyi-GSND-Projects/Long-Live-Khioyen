using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Get Unit Position")]
    public class GetUnitPositionAction : GameEventAction
    {
        [Header("Input")]
        public int targetUnitInstanceId;

        [Header("Output")]
        public string outputKey = "TargetPos";

        public override void Execute()
        {
            if (Battle.Instance == null)
            {
                Debug.LogError("[GetUnitPosition] Battle.Instance is null!");
                return;
            }
            
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentEvent == null)
            {
                Debug.LogError("[GetUnitPosition] No active event context!");
                return;
            }

            

            Unit unit = Battle.Instance.GetUnitByInstanceId(targetUnitInstanceId);
            Debug.Log($"[GetUnitPosition] Searching for Unit ID: {targetUnitInstanceId}...");
            if (unit != null)
            {   
                Debug.Log($"[GetUnitPosition] SUCCESS: Found Unit '{unit.name}' at {unit.position}. Saved to blackboard key '{outputKey}'.");
                BattleEventManager.Instance.CurrentEvent.SetData(outputKey, unit.position);
              }
            else
            {
                // 尝试列出所有在场单位 ID，方便调试
                string activeIds = "";
                //foreach(var kvp in Battle.Instance.GetUnitsByFaction(Faction.Player)) activeIds += $"{kvp.InstanceId}, ";
                //foreach(var kvp in Battle.Instance.GetUnitsByFaction(Faction.Enemy)) activeIds += $"{kvp.InstanceId}, ";
                
                Debug.LogWarning($"[GetUnitPosition] FAILED: Unit {targetUnitInstanceId} not found! Active IDs: [{activeIds}]");
            }
        }
    }
}