using UnityEngine;
using System.Collections.Generic;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class PreplacedUnitData
    {
        [Header("Identity")]
        public int instanceId;
        
        public string unitName; // 方便编辑器显示
        public Vector2Int position;
        public Faction faction;
        public bool isFacility;
        public bool isVisible = true;
        
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
        
        [Tooltip("本场战斗的胜利条件")]
        public BattleGoal battleGoal = BattleGoal.Annihilate;
        
        [Header("Spawn Points")]
        public List<Vector2Int> playerDeployPoints = new List<Vector2Int>(); // 玩家部署区
        public List<Vector2Int> extractionPoints = new List<Vector2Int>();   // 撤离点
        
        [Header("Events")]
        public List<BattleEventDefinition> levelEvents = new List<BattleEventDefinition>(); // 预设事件
        
        [Header("Random Enemy Generation (if not using fixed enemies)")]
       [Tooltip("敌人可以生成的区域")]
       public List<Vector2Int> nonPlayerUnitsSpawnZones = new List<Vector2Int>();
       [Tooltip("随机生成敌人的规则")]
       public List<RandomEnemySpawnRule> randomEnemyRules = new List<RandomEnemySpawnRule>();

       [Header("Fixed Units (if using fixed enemies)")]
        public List<PreplacedUnitData> preplacedUnits = new List<PreplacedUnitData>();
        
        [Header("Player Army Settings")]
        public bool usePresetPlayerArmy = false;
        public List<PreplacedUnitData> playerReserveList = new List<PreplacedUnitData>();
        
    }
}