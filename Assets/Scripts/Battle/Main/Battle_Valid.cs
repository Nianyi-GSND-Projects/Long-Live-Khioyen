using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
        
        private HashSet<Vector2Int> availableMovePositions;
        private HashSet<Vector2Int> availableArrangementPositions;
        private HashSet<Vector2Int> availableTargetPositions;
        
        #region Target_Check

        public bool HasAnyValidTarget(Unit user, ActionDefinition action)
        {
            if (user == null || action == null) return false;

            // 1. Self 类型：只检查自己脚下
            if (action.targetCountType == TargetCountType.Self)
            {
                return action.IsTileValidTarget(user, user.position);
            }

            // 2. 范围搜索：找到一个就返回 True
            Vector3Int centerCube = OffsetToCube(user.position);
            int N = action.range;
            int minN = action.minRange;

            for (int q = -N; q <= N; q++)
            {
                for (int r = -N; r <= N; r++)
                {
                    for (int s = -N; s <= N; s++)
                    {
                        if (q + r + s == 0)
                        {
                            int dist = (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(s)) / 2;
                            if (dist > N || dist < minN) continue;

                            Vector3Int neighborCube = centerCube + new Vector3Int(q, r, s);
                            Vector2Int neighborPos = CubeToOffset(neighborCube);

                            if (IsValidMapPosition(neighborPos))
                            {
                                // [核心优化] 只要找到一个合法的，立刻返回 true
                                if (action.IsTileValidTarget(user, neighborPos))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        #endregion

        #region Position_Check

        public bool TestAvailableMovePositions(Vector2Int mapPosition)
        {
            return availableMovePositions.Contains(mapPosition);
        }
        
        public bool IsValidMapPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y;
        }
		
        public bool IsTargetPositionValid(Vector2Int pos)
        {
            return availableTargetPositions != null && availableTargetPositions.Contains(pos);
        }
        public bool ValidateArrangementPlacement(Vector2Int placement)
        {
            if(!IsValidMapPosition(placement))
                return false;
            if (!availableArrangementPositions.Contains(placement)&&CurrentStage == Stage.Arrangement) 
                return false;
			
            UnitPassability terrainPass = hexTiles[placement].TerrainDefinition.unitPassability;
            if (terrainPass == UnitPassability.Impassable) 
                return false;
			
            if (mapData[placement.x, placement.y].Battalion != null)
                return false;
			
            return true;
        }
        #endregion
    }
}
