using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public abstract class TileEffectDefinition : ScriptableObject
    {
        public string effectName;
        public GameObject vfxPrefab;
        public bool isPassable = false; 
        
        // tile: 当前地块数据
        // pos: 坐标
        public virtual void OnTick(TileData tile, Vector2Int pos) 
        { 
            // 默认逻辑：如果格子里有单位，对单位造成影响
            if (tile.Battalion != null) ApplyEffectToUnit(tile.Battalion);
            if (tile.Facility != null) ApplyEffectToUnit(tile.Facility);
        }

        protected virtual void ApplyEffectToUnit(Unit unit) { }
        
        public virtual void OnEnter(Unit unit) { } // 单位走进格子时触发
    }
}
