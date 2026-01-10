using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Terrain/Terrain Definition")]
        public class TerrainDefinition : ScriptableObject
        {
            [Header("Identity")]
            public string terrainName;
    
            [Header("Visuals")]
            public Material material; 
            // public GameObject propPrefab;
    
            [Header("Gameplay Stats")]
            public int movementCost = 1; // 移动力消耗
            public int defenseBonus = 0; // 防御/闪避加成
            public UnitPassability unitPassability;
        }
}
