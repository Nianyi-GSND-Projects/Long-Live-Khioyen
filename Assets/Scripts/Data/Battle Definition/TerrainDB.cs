using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Terrain/Terrain Database")]
    public class TerrainDB : ScriptableObject
    {
        private const string RESOURCE_PATH = "Data/TerrainDB";
        
        private static TerrainDB _instance;
        public static TerrainDB Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试从 Resources 文件夹加载
                    _instance = Resources.Load<TerrainDB>(RESOURCE_PATH);
                    
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
        
        private Dictionary<string, TerrainDefinition> terrainDefinitionMap;

        public void Initialize()
        {
            terrainDefinitionMap = new Dictionary<string, TerrainDefinition>();
            foreach (var terrainDefinition in terrainDefinitions)
            {
                terrainDefinitionMap.Add(terrainDefinition.terrainName, terrainDefinition);
            }
        }
        
        public TerrainDefinition GetTerrain(string terrainName)
        {
            if(terrainDefinitionMap == null) Initialize();
            
            if(terrainDefinitionMap.TryGetValue(terrainName, out var terrainDefinition))
                return terrainDefinitionMap[terrainName];
            
            return null;
        }
        
        
    }
}
