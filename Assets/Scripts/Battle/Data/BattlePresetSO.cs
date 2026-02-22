using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class PreplacedUnitData
    {
        public string unitName; // 方便编辑器显示
        public Vector2Int position;
        public Faction faction;
        public bool isFacility;
        
        public BattalionDefinition battalionDef;
        public FacilityDefinition facilityDef;

        [Header("Overrides")]
        public int overrideSoldiers = -1; // -1 = Default
        public int overrideMorale = -1;   // -1 = Default
        
        [Header("Commander")]
        public CommanderTemplateSO commanderTemplate; // 指定名将
        public CommanderGenerationProfile randomCommanderProfile; // 随机生成配置
        public bool useRandomCommander = false; // 开关
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Level/Battle Preset")]
    public class BattlePresetSO : ScriptableObject
    {
        [Header("Base Map")]
        public MapDataSO mapData; // 引用之前的地形数据

        [Header("Level Config")]
        public string levelName = "New Battle";
        
        [Header("Spawn Points")]
        public List<Vector2Int> playerDeployPoints = new List<Vector2Int>(); // 玩家部署区
        public List<Vector2Int> extractionPoints = new List<Vector2Int>();   // 撤离点
        
        [Header("Events")]
        public List<BattleEventDefinition> levelEvents = new List<BattleEventDefinition>(); // 预设事件

        
        [Header("Units")]
        public List<PreplacedUnitData> preplacedUnits = new List<PreplacedUnitData>(); // 预设的敌人/中立设施
    }
}