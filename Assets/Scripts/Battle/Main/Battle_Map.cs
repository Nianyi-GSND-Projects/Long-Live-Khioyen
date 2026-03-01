using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
        public Grid hexgrid;
        public float Xscale;
        public float Yscale;
        
        public TileData[,] mapData; 
        public string[,] mapTerrainData; 
        private Dictionary<Vector2Int,HexTile> hexTiles = new();
        
        #region Generation

        void GenerateHexGrid()
        {
            Quaternion hexRotation = Quaternion.Euler(0, 30, 0);
            if(HextilePrefab == null)
            {
                Debug.LogError("Hextile prefab is not assigned!");
                return;
            }
            Transform mapContainer = new GameObject("HexMapContainer").transform;
            mapContainer.SetParent(transform, false);
			
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    Vector2Int mapPos = new Vector2Int(x, y);

                    Vector3 worldPos = MapToLocal(mapPos); 

                    GameObject tileObject = Instantiate(HextilePrefab, worldPos, hexRotation, mapContainer);
                    tileObject.name = $"Hex Tile ({x}, {y})";
            
                    HexTile hexTile = tileObject.GetComponent<HexTile>();
                    hexTile.mapPosition = mapPos;
                    hexTiles.Add(mapPos, hexTile);
                    //TODO
                    if (presetMapData != null)
                    {
                        // 从预设数据中读取地形 ID
                        string terrainId = presetMapData.GetTerrainAt(x, y);
                        AssignTerrainToTile(hexTile, terrainId);
                    }
                    else
                    {
                        // 如果没拖地图，默认全是平原
                        AssignTerrainToTile(hexTile, "Plain");
                    }
                }
            }
        }

        #endregion

        #region Terrain

        public void AssignTerrainToTile(HexTile tile, string terrainType)
        {
            TerrainDefinition def = TerrainDatabase.Instance.GetTerrain(terrainType);
            if (def != null)
            {
                tile.SetTerrain(def);
                mapTerrainData[tile.mapPosition.x, tile.mapPosition.y] = terrainType;
            }
        }

        #endregion

        #region Space

        private readonly Vector2Int[][] neighborOffsets = new Vector2Int[][]
        {
            // 偶数行 (y % 2 == 0) 的邻居偏移
            new Vector2Int[] 
            { 
                new Vector2Int(0, 1),  // 右上
                new Vector2Int(1, 0),  // 右
                new Vector2Int(-1, -1), // 右下
                new Vector2Int(0, -1), // 左下
                new Vector2Int(-1, 0), // 左
                new Vector2Int(-1, 1)   // 左上
            },
            // 奇数行 (y % 2 != 0) 的邻居偏移
            new Vector2Int[] 
            { 
                new Vector2Int(1, 1),   // 右上
                new Vector2Int(1, 0),   // 右
                new Vector2Int(1, -1),  // 右下
                new Vector2Int(0, -1), // 左下
                new Vector2Int(-1, 0),  // 左
                new Vector2Int(0, 1)  // 左上
            }
        };
        public Vector2 WorldToMap(Vector3 world)
        {
            Vector3Int gridPos = hexgrid.WorldToCell(world);
            return new(
                gridPos.x ,
                gridPos.y 
            );
        }
        public Vector2Int WorldToMapInt(Vector3 world)
        {
            //return Vector2Int.FloorToInt(WorldToMap(world));
            Vector3Int gridPos = hexgrid.WorldToCell(world);
            return new Vector2Int(gridPos.x, gridPos.y);
        }
        public Vector3 MapToWorld(Vector2Int map)
        {
            //return transform.localToWorldMatrix.MultiplyPoint(MapToLocal(map));
            Vector3Int gridPos = new Vector3Int(map.x, map.y, 0);
            return hexgrid.GetCellCenterWorld(gridPos);
        }
        public Vector3 MapToLocal(Vector2 map)
        {
            return hexgrid.CellToLocalInterpolated(new(
                map.x,
                map.y,
                0
            ));
        }
        
        private Vector3Int OffsetToCube(Vector2Int hex)
        {
            var q = hex.x - (hex.y - (hex.y & 1)) / 2;
            var r = hex.y;
            return new Vector3Int(q, r, -q - r);
        }
        
        public Vector2Int CubeToOffset(Vector3Int cube)
        {
            var col = cube.x + (cube.y - (cube.y & 1)) / 2;
            var row = cube.y;
            return new Vector2Int(col, row);
        }
        #endregion

        #region Zone

        public void CreateExtractionPoint(Vector2Int pos)
        {
            if (!IsValidMapPosition(pos)) return;
            
            TileData tile = mapData[pos.x, pos.y];
            tile.isExtractionPoint = true;

            // 生成永久视觉标记 (不同于那些临时的高亮格子)
            // 假设你有一个 extractionPointPrefab
            if (extractionPointPrefab != null) // 记得在 Battle 中加这个变量并拖拽 Prefab
            {
                Vector3 worldPos = MapToLocal(pos);
                // 稍微抬高一点防止穿模
                GameObject vfx = Instantiate(extractionPointPrefab, transform);
                vfx.transform.localPosition = worldPos;
                
                // 记录下来，方便以后可能的移除
                tile.TileVFX = vfx; 
            }
        }
        
        
        void GenerateArrangementSlot()
        {
            //TODO:根据玩家进入战斗的角度，在合适的位置创建部署区
            for(int i= 0;i < 3;i++)
            for (int j = 0; j < 3; j++)
            {
                availableArrangementPositions.Add(new Vector2Int(i, j));
            }
        }

        #endregion
        
        #region Accessibility


        
        public bool CanUnitStopOnTile(Unit unit, Vector2Int pos,bool checkVisibility)
        {
            if (!IsValidMapPosition(pos)) return false;
            TileData tile = mapData[pos.x, pos.y];
            if (tile.Battalion && tile.Battalion != unit)
            {
                if (checkVisibility && !tile.Battalion.IsVisible)
                {
                }
                else
                {
                    return false;
                }
            }
            if (tile.Battalion&& tile.Battalion != unit) return false;
			
            //假如目标地点有设施，则设施的可通行性覆盖地形本身的可通行性
            //否则，考虑地形本身的可通行性
            UnitPassability p;
            if (tile.Facility&&(!checkVisibility||tile.Facility.IsVisible||tile.Facility.faction==unit.faction))
            { 
                p = tile.Facility.Definition.passability;
            }
            else p = hexTiles[pos].TerrainDefinition.unitPassability;

            return p switch
            {
                UnitPassability.Impassable => false,
                UnitPassability.Passable => false,
                UnitPassability.AlliesPassable => false,
                UnitPassability.Stoppable => true,
                UnitPassability.AlliesStoppable => tile.Facility.faction == unit.faction,
                _ => true,
            };
        }
		
        public bool CanUnitPassThroughTile(Unit unit, Vector2Int pos,bool checkVisibility = false)
        {
            if (!IsValidMapPosition(pos)) return false;
            TileData tile = mapData[pos.x, pos.y];
            if (tile.Battalion)
            {
                if (tile.Battalion.faction == unit.faction)
                {
                    if (tile.Battalion.Definition.passability == UnitPassability.Impassable) return false;
                    return true;
                }
                else 
                {
                    if (checkVisibility && !tile.Battalion.IsVisible)
                    {
                        return true;
                    }
                    return false; // 实际移动时，或看得见时，不可穿过
                }
            }
            
            UnitPassability p;
            if (tile.Facility&&(!checkVisibility||tile.Facility.IsVisible||tile.Facility.faction==unit.faction))
            { 
                p = tile.Facility.Definition.passability;
            }
            else p = hexTiles[pos].TerrainDefinition.unitPassability;

            return p switch
            {
                UnitPassability.Impassable => false,
                UnitPassability.Passable => true,
                UnitPassability.Stoppable => true,
                UnitPassability.AlliesPassable or UnitPassability.AlliesStoppable => tile.Facility.faction == unit.faction,
                _ => true,
            };
            
        }

        #endregion

        #region Function

        public int GetHexDistance(Vector2Int a, Vector2Int b)
        {
            Vector3Int ac = OffsetToCube(a);
            Vector3Int bc = OffsetToCube(b);
            return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) / 2;
        }
        
        public Vector3Int GetHexDirection(Vector2Int start, Vector2Int target)
        {
            Vector3Int startCube = OffsetToCube(start);
            Vector3Int targetCube = OffsetToCube(target);
            Vector3Int diff = targetCube - startCube;
			
            int len = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));
            if (len == 0) return Vector3Int.zero;

            return new Vector3Int(diff.x / len, diff.y / len, diff.z / len);
        }
        
        public Vector2Int GetTileInDirection(Vector2Int start, Vector3Int directionCube, int distance)
        {
            Vector3Int startCube = OffsetToCube(start);
            Vector3Int destCube = startCube + (directionCube * distance);
            return CubeToOffset(destCube);
        }
        
        public HashSet<Vector2Int> GetAllTilesInRange(Vector2Int startPos, int range)
        {
            HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();
			
            if (!hexTiles.ContainsKey(startPos))
            {
                Debug.LogWarning($"Function GetAllTilesInRange: 基准位置 {startPos} 不存在。");
                return reachableTiles;
            }
			
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(startPos);
			
            Dictionary<Vector2Int, int> distanceTravelled = new Dictionary<Vector2Int, int>();
            distanceTravelled[startPos] = 0;
			
            while (frontier.Count > 0)
            {
                Vector2Int currentPos = frontier.Dequeue();
				
                reachableTiles.Add(currentPos);
				
                if (distanceTravelled[currentPos] >= range) continue;
				
                int parity = currentPos.y & 1;
                foreach (var offset in neighborOffsets[parity])
                {
                    Vector2Int neighborPos = currentPos + offset;

                    if (hexTiles.ContainsKey(neighborPos) && !distanceTravelled.ContainsKey(neighborPos))
                    {
                        distanceTravelled[neighborPos] = distanceTravelled[currentPos] + 1;
                        frontier.Enqueue(neighborPos);
                    }
                }
            }
    
            return reachableTiles;
        }
        #endregion
        
        private Vector2Int GetRandomValidPosition(UnitPassability passability)
        {
            int x = Random.Range(0, Size.x);
            int y = Random.Range(0, Size.y);
			
            int attempts = 0;
            int maxAttempts = 1000;
			
            while (attempts < maxAttempts)
            {
                Vector2Int pos = new Vector2Int(x, y);
                
                // 1. 检查格子是否有单位 (现有逻辑)
                bool isOccupied = mapData[x, y].Battalion != null || mapData[x, y].Facility != null;

                UnitPassability terrainPass = hexTiles[pos].TerrainDefinition.unitPassability;
                bool isTerrainWalkable = (terrainPass == UnitPassability.Stoppable || terrainPass == UnitPassability.Passable); 

                if (!isOccupied && isTerrainWalkable)
                {
                    return pos;
                }

                // 重试
                x = Random.Range(0, Size.x);
                y = Random.Range(0, Size.y);
                attempts++;
            }
			
            return new Vector2Int(x,y);
        }

        #region Tile Effect

        public void AddTileEffect(Vector2Int pos, TileEffectDefinition def, int duration, Unit source)
        {
            if (!IsValidMapPosition(pos)) return;
            TileData tile = mapData[pos.x, pos.y];

            // 1. 创建数据实例
            TileEffect effect = new TileEffect(def, duration, source);

            // 2. 生成视觉特效
            if (def.vfxPrefab != null)
            {
                Vector3 worldPos = MapToLocal(pos);
                // 稍微抬高一点防止穿模，或者依靠Prefab自带偏移
                GameObject vfx = Instantiate(def.vfxPrefab, transform); 
                vfx.transform.localPosition = worldPos;
                effect.vfxInstance = vfx;
            }

            // 3. 加入数据
            tile.Effects.Add(effect);
            Debug.Log($"Tile {pos} added effect: {def.effectName}");
        }
		
        public void UpdateAllTileEffects()
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    UpdateTileEffectsAt(new Vector2Int(x, y));
                }
            }
        }
		
        private void UpdateTileEffectsAt(Vector2Int pos)
        {
            TileData tile = mapData[pos.x, pos.y];
            if (tile.Effects.Count == 0) return;

            for (int i = tile.Effects.Count - 1; i >= 0; i--)
            {
                TileEffect effect = tile.Effects[i];

                if (effect.definition != null)
                {
                    effect.definition.OnTick(tile, pos);
                }

                effect.currentDuration--;
                if (effect.currentDuration <= 0)
                {
                    // 销毁特效物体
                    if (effect.vfxInstance != null) Destroy(effect.vfxInstance);
                    tile.Effects.RemoveAt(i);
                }
            }
        }


        #endregion

        #region Trigger

        public bool CheckTileEffectOnEnter(Unit unit, Vector2Int pos)
        {
            if (!IsValidMapPosition(pos)) return false;

            bool PreventMovement = false;
            TileData tile = mapData[pos.x, pos.y];

            if (tile.Facility != null && tile.Facility.Definition is TrapFacilityDefinition trapDef)
            {
                trapDef.Trigger(unit, tile.Facility);
                if (unit.currentHealth <= 0 || trapDef.PreventMovement)
                    PreventMovement = true;
            }

            if (tile.Effects.Count > 0)
            {
                var effectsToCheck = new List<TileEffect>(tile.Effects);
                foreach (var effect in effectsToCheck)
                {
                    if (effect.definition != null)
                    {
                        effect.definition.OnEnter(unit);
                    }
                }
                
                if (unit.currentHealth <= 0)
                {
                    PreventMovement = true;
                }
            }

            return PreventMovement;
        }
        
        public int CalculateExtraMoveCost(Unit unit, Vector2Int pos)
        {
            if (!IsValidMapPosition(pos)) return 999; // 无法进入

            int extraCost = 0;
            
            // 1. 检查 ZOC (之后实现)
            // extraCost += GetZOCCost(unit, pos);
            
            // 2. 检查地形消耗
            // extraCost += GetTerrainCost(pos);

            return extraCost;
        }

        #endregion
    }
}
