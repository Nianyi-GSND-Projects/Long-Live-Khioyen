using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
        #region PathFind

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, Unit unit)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            if (start == end) return path;

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);
			
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start; // 标记起点

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();

                if (current == end) break;

                int parity = current.y & 1;
                foreach (var offset in neighborOffsets[parity])
                {
                    Vector2Int next = current + offset;
					
                    if (!IsValidMapPosition(next)) continue;
                    if (cameFrom.ContainsKey(next)) continue;
					
                    // 检查通行性 (如果是终点，允许停留检查；如果是中间点，允许穿过检查)
                    bool isEnd = (next == end);
                    if (isEnd)
                    {
                        if (!CanUnitStopOnTile(unit, next)) continue;
                    }
                    else
                    {
                        if (!CanUnitPassThroughTile(unit, next)) continue;
                    }

                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }

            if (!cameFrom.ContainsKey(end)) return null; // 无法到达

            // 重建路径
            Vector2Int curr = end;
            while (curr != start)
            {
                path.Add(curr);
                curr = cameFrom[curr];
            }
            path.Reverse();
            return path;
        }
        
        public HashSet<Vector2Int> GetAccessableTilesInRange(Unit movingUnit, int range)
        {
			
            HashSet<Vector2Int> validDestinations = new HashSet<Vector2Int>();
            if (!movingUnit) return validDestinations;
			
            Vector2Int startPos = movingUnit.position;
			
            validDestinations.Add(startPos); 
            if (!IsValidMapPosition(startPos)) return validDestinations;
			
			
            if (!hexTiles.ContainsKey(startPos))
            {
                Debug.LogWarning($"尝试从一个不存在的格子 {startPos} 开始寻路。");
                return validDestinations;
            }
			
            HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(startPos);
			
            Dictionary<Vector2Int, int> CostSofar = new Dictionary<Vector2Int, int>();
            CostSofar[startPos] = 0;
			
            while (frontier.Count > 0)
            {
                Vector2Int currentPos = frontier.Dequeue();

                if (currentPos != startPos)
                {
                    if(CanUnitStopOnTile(movingUnit,currentPos))
                        validDestinations.Add(currentPos);
                }
                if(CanUnitPassThroughTile(movingUnit, currentPos)) reachableTiles.Add(currentPos);
				
                if (CostSofar[currentPos] >= range) continue;
				
                int parity = currentPos.y & 1;
                foreach (var offset in neighborOffsets[parity])
                {
                    Vector2Int neighborPos = currentPos + offset;
					
                    if (!CanUnitPassThroughTile(movingUnit, neighborPos)) continue;
 
                    // int moveCost = TerrainDatabase.Instance.GetTerrain(mapTerrainData[neighborPos.x, neighborPos.y]).movementCost;
                    int moveCost = 1; 
                    int newCost = CostSofar[currentPos] + moveCost;

                    if (newCost <= range && !CostSofar.ContainsKey(neighborPos))
                    {
                        CostSofar[neighborPos] = newCost;
                        frontier.Enqueue(neighborPos);
                    }
                }
            }

            return validDestinations;
        }



        #endregion

        #region TargetSearch

        public HashSet<Vector2Int> GetValidActionTargetTiles(Unit user, ActionDefinition action)
        {
            HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
            
            // 如果是 Self 类型，只返回自己脚下
            if (action.targetCountType == TargetCountType.Self)
            {
                validTiles.Add(user.position);
                return validTiles;
            }
            
            Vector3Int centerCube = OffsetToCube(user.position);
            int N = action.range;
            int minN = action.minRange; // 假设你有最小射程

            for (int q = -N; q <= N; q++)
            {
                for (int r = -N; r <= N; r++)
                {
                    for (int s = -N; s <= N; s++)
                    {
                        if (q + r + s == 0)
                        {
                            // 计算距离
                            int dist = (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(s)) / 2;
                            if (dist > N || dist < minN) continue;

                            // 转换回 Offset 坐标
                            Vector3Int neighborCube = centerCube + new Vector3Int(q, r, s);
                            Vector2Int neighborPos = CubeToOffset(neighborCube); // 需要添加 CubeToOffset 辅助方法

                            if (IsValidMapPosition(neighborPos))
                            {
                                if (action.IsTileValidTarget(user, neighborPos))
                                {
                                    validTiles.Add(neighborPos);
                                }
                            }
                        }
                    }
                }
            }
            return validTiles;
        }
        
        public Unit FindNearestUnit(Unit source, Faction targetFaction, UnitTypeFilter typeFilter = UnitTypeFilter.All)
        {
            Unit nearest = null;
            int minDist = int.MaxValue;

            var targets = GetUnitsByFaction(targetFaction);

            foreach (var targetUnit in targets)
            {
                if (targetUnit == null || !targetUnit.gameObject.activeSelf) continue;

                bool isTypeMatch = false;
                switch (typeFilter)
                {
                    case UnitTypeFilter.All:
                        isTypeMatch = true;
                        break;
                    case UnitTypeFilter.BattalionOnly:
                        isTypeMatch = (targetUnit is Battalion);
                        break;
                    case UnitTypeFilter.FacilityOnly:
                        isTypeMatch = (targetUnit is Facility);
                        break;
                }
				
                if (!isTypeMatch) continue;
				
                int d = GetHexDistance(source.position, targetUnit.position);
        
                if (d < minDist)
                {
                    minDist = d;
                    nearest = targetUnit;
                }
            }
            return nearest;
        }

        #endregion
    }
}
