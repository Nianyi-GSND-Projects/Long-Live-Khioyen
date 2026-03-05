// Assets/Scripts/Battle/Visual/FogOfWarController.cs (新文件)

using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class FogOfWarController : MonoBehaviour
    {
        public GameObject fogTilePrefab; // 一个简单的带 SpriteRenderer 的 Prefab
        
        private Dictionary<Vector2Int, SpriteRenderer> _fogTiles = new Dictionary<Vector2Int, SpriteRenderer>();
        
        public Color concealedColor = Color.black;
        public Color exploredColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        public Color visibleColor = Color.clear;

        public void Initialize(Vector2Int mapSize)
        {
            if (fogTilePrefab == null)
            {
                Debug.LogError("FogTilePrefab is not assigned!");
                return;
            }

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Vector3 worldPos = Battle.Instance.MapToWorld(pos);
                    
                    GameObject fogGO = Instantiate(fogTilePrefab, worldPos, Quaternion.identity, transform);
                    fogGO.name = $"Fog_{x}_{y}";
                    
                    SpriteRenderer sr = fogGO.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        _fogTiles[pos] = sr;
                    }
                }
            }
        }

        public void UpdateFogVisuals(FogState[,] fogMap)
        {
            if (fogMap == null) return;

            foreach (var kvp in _fogTiles)
            {
                Vector2Int pos = kvp.Key;
                SpriteRenderer sr = kvp.Value;
                
                FogState state = fogMap[pos.x, pos.y];
                switch (state)
                {
                    case FogState.Concealed:
                        sr.color = concealedColor;
                        break;
                    case FogState.Explored:
                        sr.color = exploredColor;
                        break;
                    case FogState.Visible:
                        sr.color = visibleColor;
                        break;
                }
            }
        }
    }
}