using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Database/Terrain Database")]
    public class TerrainDatabase : ScriptableObject
    {
        private const string RESOURCE_PATH = "Data/TerrainDatabase";

        private static TerrainDatabase _instance;

        public static TerrainDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试从 Resources 文件夹加载
                    _instance = Resources.Load<TerrainDatabase>(RESOURCE_PATH);

                    if (_instance == null)
                    {
                        Debug.LogError($"【致命错误】找不到地形数据库！请确保文件位于 'Assets/Resources/{RESOURCE_PATH}'");
                        return null;
                    }

                    // 加载后立即初始化字典
                    _instance.Initialize();
                }

                return _instance;
            }
        }

        public List<TerrainDefinition> terrainDefinitions;

        // 原有的基于名字的查找表
        private Dictionary<string, TerrainDefinition> terrainDefinitionMap;

        // [新增] 基于 ID 的查找表
        private Dictionary<int, TerrainDefinition> _idLookup;

        public void Initialize()
        {
            terrainDefinitionMap = new Dictionary<string, TerrainDefinition>();
            _idLookup = new Dictionary<int, TerrainDefinition>(); // 初始化 ID 字典

            foreach (var terrainDefinition in terrainDefinitions)
            {
                if (terrainDefinition == null) continue;

                // 填充名字字典
                if (!terrainDefinitionMap.ContainsKey(terrainDefinition.terrainName))
                {
                    terrainDefinitionMap.Add(terrainDefinition.terrainName, terrainDefinition);
                }

                // [新增] 填充 ID 字典
                if (!_idLookup.ContainsKey(terrainDefinition.id))
                {
                    _idLookup.Add(terrainDefinition.id, terrainDefinition);
                }
                else
                {
                    Debug.LogWarning(
                        $"TerrainDatabase: Duplicate ID {terrainDefinition.id} for {terrainDefinition.terrainName}");
                }
            }
        }

        public TerrainDefinition GetTerrain(string terrainName)
        {
            if (terrainDefinitionMap == null) Initialize();

            if (terrainDefinitionMap.TryGetValue(terrainName, out var terrainDefinition))
                return terrainDefinition;

            return null;
        }

        // [新增] 基于 ID 获取地形
        public TerrainDefinition GetTerrain(int id)
        {
            if (_idLookup == null) Initialize();

            if (_idLookup.TryGetValue(id, out var terrain))
            {
                return terrain;
            }

            Debug.LogWarning($"TerrainDatabase: Terrain ID {id} not found.");
            return null;
        }
    }
}
