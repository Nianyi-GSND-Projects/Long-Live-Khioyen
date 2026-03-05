using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Node 
    {
        public Vector2Int position;
        public int cost;
        public float priority; // 用于 A*
        public Node(Vector2Int pos, int c) { position = pos; cost = c; }
    }
    public partial class Battle
    {
        #region PathFind

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, Unit unit, bool checkVisibility = true)
        {
            if (start == end) return new List<Vector2Int>();

            // 使用一个简单的 List 作为优先队列，每次查找并移除 F 值最小的节点
            List<Node> frontier = new List<Node>();
            frontier.Add(new Node(start, 0));

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
            cameFrom[start] = start;
            costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                // 找到 F 值最小的节点
                Node current = null;
                float minPriority = float.MaxValue;
                foreach (var node in frontier)
                {
                    if (node.priority < minPriority)
                    {
                        minPriority = node.priority;
                        current = node;
                    }
                }
                frontier.Remove(current);

                if (current.position == end)
                {
                    // 找到终点，重建路径
                    return ReconstructPath(cameFrom, start, end);
                }

                // 遍历邻居
                int parity = current.position.y & 1;
                foreach (var offset in neighborOffsets[parity])
                {
                    Vector2Int next = current.position + offset;

                    if (!IsValidMapPosition(next)) continue;

                    // 检查通行性
                    bool isEnd = (next == end);
                    if (isEnd)
                    {
                        if (!CanUnitStopOnTile(unit, next, checkVisibility)) continue;
                    }
                    else
                    {
                        if (!CanUnitPassThroughTile(unit, next, checkVisibility)) continue;
                    }

                    // 计算新的 G-cost (实际消耗)
                    int moveCostToNext = 1 + CalculateVisualExtraMoveCost(unit, next);
                    int newCost = costSoFar[current.position] + moveCostToNext;

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        // 计算 F-cost (G + H)
                        float priority = newCost + GetHexDistance(next, end);
                        
                        var nextNode = new Node(next, newCost) { priority = priority };
                        frontier.Add(nextNode);
                        cameFrom[next] = current.position;
                    }
                }
            }

            return null; // 无法到达
        }
        
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = end;
            while (current != start)
            {
                path.Add(current);
                if (!cameFrom.ContainsKey(current))
                {
                    return null; 
                }
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }
        
        public HashSet<Vector2Int> GetAccessableTilesInRange(Unit movingUnit, int range,bool checkVisibility = true)
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
                    if(CanUnitStopOnTile(movingUnit,currentPos,checkVisibility))
                        validDestinations.Add(currentPos);
                }
                if(CanUnitPassThroughTile(movingUnit, currentPos,checkVisibility)) reachableTiles.Add(currentPos);
				
                if (CostSofar[currentPos] >= range) continue;
				
                int parity = currentPos.y & 1;
                foreach (var offset in neighborOffsets[parity])
                {
                    Vector2Int neighborPos = currentPos + offset;
					
                    if (!CanUnitPassThroughTile(movingUnit, neighborPos,checkVisibility)) continue;
 
                    int moveCost = 1; 
                    
                    moveCost += CalculateVisualExtraMoveCost(movingUnit, neighborPos);
                    
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
        public List<Unit> FindVisibleOpponentsInVision(Unit aiUnit)
        {
            List<Unit> visibleOpponents = new List<Unit>();
            if (aiUnit == null) return visibleOpponents;

            int visionRange = aiUnit.GetVisionRange();
            var tilesInVision = GetAllTilesInRange(aiUnit.position, visionRange);

            // 2. 确定目标阵营
            Faction targetFaction = (aiUnit.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy;
            // 也可以包含友军
            // Faction secondaryTargetFaction = (aiUnit.faction == Faction.Enemy) ? Faction.Friend : Faction.SomeOtherAI;

            // 3. 遍历视野内的格子
            foreach (var pos in tilesInVision)
            {
                var tileData = mapData[pos.x, pos.y];
                Unit unitOnTile = tileData.Battalion ?? (Unit)tileData.Facility;

                if (unitOnTile != null && unitOnTile.faction == targetFaction && unitOnTile.IsVisible)
                {
                    // 这个单位是可见的敌方单位
                    visibleOpponents.Add(unitOnTile);
                }
            }
            return visibleOpponents;
        }

        public HashSet<Vector2Int> GetValidActionTargetTiles(Unit user, ActionDefinition action)
        {
            HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
            HashSet<Vector2Int> allTilesInRange = GetAllTilesInRange(user.position, action.maxRange);
            
            // 如果是 Self 类型，只返回自己脚下
            if (action.targetCountType == TargetCountType.Self)
            {
                validTiles.Add(user.position);
                return validTiles;
            }
            
            Vector3Int centerCube = OffsetToCube(user.position);
            int N = action.maxRange;
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
