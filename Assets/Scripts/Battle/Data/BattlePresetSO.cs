using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [System.Serializable]
    public class UnitSpawnData
    {
        public Vector2Int position;
        public Faction faction;
        
        // 区分是部队还是设施
        public bool isFacility; 
        
        // 数据引用
        public BattalionDefinition battalionDef; // 如果是部队
        public FacilityDefinition facilityDef;   // 如果是设施
        
        // 可选：指挥官、初始兵力等覆盖数据
        public int overrideSoldiers = -1;
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Level/Battle Preset")]
    public class BattlePresetSO : ScriptableObject
    {
        [Header("Base Map")]
        public MapDataSO mapData; // 引用之前的地形数据

        [Header("Level Config")]
        public string levelName = "New Battle";
        
        [Header("Spawn Points")]
        public List<Vector2Int> playerDeployPoints = new List<Vector2Int>(); // 玩家出生点/布阵格
        public List<Vector2Int> extractionPoints = new List<Vector2Int>();   // 撤离点

        [Header("Units")]
        public List<UnitSpawnData> preplacedUnits = new List<UnitSpawnData>(); // 预设的敌人/中立设施
    }
}