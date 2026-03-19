using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Terrain Definition")]
        public class TerrainDefinition : ScriptableObject
        {
            [Header("Database Info")]
            public int id;
            public string terrainName;
    
            [Header("Visuals")]
            public Material material; 
            // public GameObject propPrefab;
            
            [Tooltip("用于在网格面上显示的地形贴图")]
            public Texture2D terrainTexture;
    
            [Header("Gameplay Stats")]
            public int movementCost = 1; // 移动力消耗
            public int defenseBonus = 0; // 防御/闪避加成
            public UnitPassability unitPassability;
        }
}
