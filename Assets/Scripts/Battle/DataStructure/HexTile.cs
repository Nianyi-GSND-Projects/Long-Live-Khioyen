using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class HexTile : MonoBehaviour
    {
        public Vector2Int mapPosition { get; set; }
        
        private TerrainDefinition terrainDefinition;
        public TerrainDefinition TerrainDefinition => terrainDefinition;
        
        private Renderer tileRenderer;
        
        [Header("Visual Components")]
        public SpriteRenderer overlayRenderer; // 实心 (ZOC, Target)
        public SpriteRenderer ringRenderer;
        
        private bool _isExtractionPoint;
        private Color _highlightColor = Color.clear;
        private Color _extractionColor = Color.clear;
        
        
        private void Awake()
        {
            tileRenderer = GetComponent<Renderer>();
            
        }

        public void SetTerrain(TerrainDefinition terrainDefinition)
        {
            this.terrainDefinition = terrainDefinition;
            if(tileRenderer!=null && terrainDefinition.material!=null) tileRenderer.sharedMaterial = terrainDefinition.material;
            
        }
        
        public void Highlight(Color color)
        {
            SetRingColor(color);
        }
        
        public void UnHighlight()
        {
            SetRingColor(Color.clear);
        }
        
        public void SetOverlayColor(Color color)
        {
            if (overlayRenderer != null)
            {
                overlayRenderer.color = color;
                overlayRenderer.gameObject.SetActive(color.a > 0);
            }
        }
        
        public void SetExtractionPoint(bool isEp, Color color)
        {
            _isExtractionPoint = isEp;
            _extractionColor = color;
            UpdateRingVisual();
        }

        public void SetRingColor(Color color) // 用于临时高亮 (Move, Deploy)
        {
            _highlightColor = color;
            UpdateRingVisual();
        }
        
        private void UpdateRingVisual()
        {
            if (ringRenderer == null) return;

            // 优先级：高亮 > 撤离点
            if (_highlightColor.a > 0)
            {
                ringRenderer.color = _highlightColor;
                ringRenderer.gameObject.SetActive(true);
            }
            else if (_isExtractionPoint)
            {
                ringRenderer.color = _extractionColor;
                ringRenderer.gameObject.SetActive(true);
            }
            else
            {
                ringRenderer.gameObject.SetActive(false);
            }
        }
    }
}
