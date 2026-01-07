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
        private Color originalColor;
        //private Color HighlightColor = Color.green;

        private void Awake()
        {
            tileRenderer = GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                originalColor = tileRenderer.material.color;
            }
        }

        public void SetTerrain(TerrainDefinition terrainDefinition)
        {
            this.terrainDefinition = terrainDefinition;
            if(tileRenderer!=null && terrainDefinition.material!=null) tileRenderer.sharedMaterial = terrainDefinition.material;
            
        }
        
        public void Highlight(Color HighlightColor)
        {
            if (tileRenderer != null)
            {
                tileRenderer.material.color = HighlightColor;
            }
        }
        
        public void UnHighlight()
        {
            if (tileRenderer != null)
            {
                tileRenderer.sharedMaterial = terrainDefinition.material;
            }
        }
    }
}
