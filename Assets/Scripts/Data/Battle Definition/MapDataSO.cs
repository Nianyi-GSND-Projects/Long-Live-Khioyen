using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Game/Map/Map Data")]
    public class MapDataSO : ScriptableObject
    {
        public int width;
        public int height;
        
        public string[] terrainIds; 

        public string GetTerrainAt(int x, int y)
        {
            int index = y * width + x;
            if (index >= 0 && index < terrainIds.Length)
                return terrainIds[index];
            return "Plain";
        }
    }
}