using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class FogOfWarController : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject fogTilePrefab;
        
        [Header("Colors")]
        [Tooltip("未探索状态下，底部六边形覆盖层的深色")]
        public Color concealedOverlayColor = Color.black; 
        [Tooltip("已探索(但不在视野内)状态下，底部六边形覆盖层的颜色")]
        public Color exploredColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        [Tooltip("完全可见时的颜色(透明)")]
        public Color visibleColor = Color.clear;
        
        [Header("Animation Settings")]
        public float fadeDuration = 0.5f;
        public Vector3 concealedScale = Vector3.one * 1.1f;
        public Vector3 visibleScale = Vector3.one * 1.5f;
        
        [Header("Positioning")]
        [Tooltip("实例化迷雾格子时的位置偏移量，用于对齐地图地形。")]
        public Vector3 fogOffset = new Vector3(0, 0.1f, 0);
        
        private class FogTileData
        {
            public SpriteRenderer cloudRenderer;
            public SpriteRenderer overlayRenderer;
            public Transform cloudTransform; 
            
            // 用于保存该格子 Cloud 的原始 Sprite 颜色
            public Color originalCloudColor; 
            
            public FogState currentState = FogState.Concealed;
            public Coroutine currentCoroutine;
        }
        
        private Dictionary<Vector2Int, FogTileData> _fogTiles = new Dictionary<Vector2Int, FogTileData>();
        
        public void Initialize(Vector2Int mapSize)
        {
            if (fogTilePrefab == null)
            {
                Debug.LogError("FogTilePrefab is not assigned in FogOfWarController!");
                return;
            }
            
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    Vector3 worldPos = Battle.Instance.MapToWorld(pos);
                    worldPos += fogOffset;
                    
                    GameObject fogGO = Instantiate(fogTilePrefab, worldPos, Quaternion.identity, transform);
                    fogGO.name = $"Fog_{x}_{y}";
                    
                    FogTile fogTile = fogGO.GetComponent<FogTile>();

                    if (fogTile != null && fogTile.cloudRenderer != null && fogTile.overlayRenderer != null)
                    {
                        // 读取并锁定 Cloud 的原始颜色
                        Color baseCloudColor = fogTile.cloudRenderer.color;
                        baseCloudColor.a = 1f; 

                        // 【修改点】初始状态：Cloud 保持原色，Overlay 设为全新的深色变量
                        fogTile.cloudRenderer.color = baseCloudColor;
                        fogTile.overlayRenderer.color = concealedOverlayColor;
                        fogTile.cloudTransform.localScale = concealedScale;

                        _fogTiles[pos] = new FogTileData 
                        { 
                            cloudRenderer = fogTile.cloudRenderer, 
                            overlayRenderer = fogTile.overlayRenderer,
                            cloudTransform = fogTile.cloudTransform,
                            originalCloudColor = baseCloudColor, 
                            currentState = FogState.Concealed 
                        };
                    }
                    else
                    {
                        Debug.LogError($"FogTile component or its references are missing on prefab at {x},{y}!");
                    }
                }
            }
        }

        public void UpdateFogVisuals(FogState[,] fogMap, bool immediate = false)
        {
            if (fogMap == null) return;

            foreach (var kvp in _fogTiles)
            {
                Vector2Int pos = kvp.Key;
                FogTileData tileData = kvp.Value;
                FogState targetState = fogMap[pos.x, pos.y];

                if (tileData.currentState != targetState)
                {
                    if (tileData.currentCoroutine != null)
                    {
                        StopCoroutine(tileData.currentCoroutine);
                    }

                    GetTargetVisuals(targetState, tileData, out Color targetCloudColor, out Color targetOverlayColor, out Vector3 targetCloudScale);

                    if (immediate)
                    {
                        tileData.cloudRenderer.color = targetCloudColor;
                        tileData.overlayRenderer.color = targetOverlayColor;
                        tileData.cloudTransform.localScale = targetCloudScale;
                    }
                    else
                    {
                        tileData.currentCoroutine = StartCoroutine(AnimateFogTransition(tileData, targetCloudColor, targetOverlayColor, targetCloudScale));
                    }

                    tileData.currentState = targetState;
                }
            }
        }

        private void GetTargetVisuals(FogState state, FogTileData tileData, out Color cloudColor, out Color overlayColor, out Vector3 cloudScale)
        {
            Color baseCloud = tileData.originalCloudColor;
            Color clearCloud = new Color(baseCloud.r, baseCloud.g, baseCloud.b, 0f);

            switch (state)
            {
                case FogState.Concealed:
                    cloudColor = baseCloud; 
                    // 【修改点】未探索时，Overlay 使用指定的深色
                    overlayColor = concealedOverlayColor; 
                    cloudScale = concealedScale;
                    break;
                case FogState.Explored:
                    cloudColor = clearCloud; 
                    overlayColor = exploredColor; 
                    cloudScale = visibleScale;   
                    break;
                case FogState.Visible:
                    cloudColor = clearCloud; 
                    overlayColor = visibleColor; 
                    cloudScale = visibleScale;
                    break;
                default:
                    cloudColor = clearCloud;
                    overlayColor = visibleColor;
                    cloudScale = visibleScale;
                    break;
            }
        }
        
        private IEnumerator AnimateFogTransition(FogTileData tileData, Color targetCloudColor, Color targetOverlayColor, Vector3 targetCloudScale)
        {
            Color startCloudColor = tileData.cloudRenderer.color;
            Color startOverlayColor = tileData.overlayRenderer.color;
            Vector3 startCloudScale = tileData.cloudTransform.localScale;
            
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime / fadeDuration;
                
                tileData.cloudRenderer.color = Color.Lerp(startCloudColor, targetCloudColor, t);
                tileData.overlayRenderer.color = Color.Lerp(startOverlayColor, targetOverlayColor, t);
                tileData.cloudTransform.localScale = Vector3.Lerp(startCloudScale, targetCloudScale, t);
                
                yield return null;
            }
            
            tileData.cloudRenderer.color = targetCloudColor;
            tileData.overlayRenderer.color = targetOverlayColor;
            tileData.cloudTransform.localScale = targetCloudScale;
        }
    }
}