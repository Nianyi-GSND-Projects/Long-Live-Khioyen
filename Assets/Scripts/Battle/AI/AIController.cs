using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class AIController : MonoBehaviour
    {
        public static AIController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public IEnumerator ProcessTurn(List<Unit> aiUnits)
        {
            Debug.Log($"[AI] Start Turn. Units count: {aiUnits.Count}");
            foreach (var unit in aiUnits)
            {
                if (Battle.Instance.IsEventBlockingAI)
                {
                    Debug.Log("[AI] Paused by Event...");
                    yield return Battle.Instance.WaitForEventBlocking();
                    Debug.Log("[AI] Resumed.");
                }
                
                if (unit == null || !unit.gameObject.activeSelf) continue;
                if (unit is not Battalion aiBattalion) continue;
                if (aiBattalion.actionDone) continue;

                Debug.Log($"[AI] Processing Unit: {unit.name} (ID:{unit.InstanceId}) at {unit.position}");

                // 模拟思考时间
                yield return new WaitForSeconds(0.5f);

                // 执行 AI 决策
                yield return StartCoroutine(ExecuteUnitAction(aiBattalion));
                
                // 再次刷新，确保状态同步
                if (Battle.Instance != null) Battle.Instance.ResolveDirtyUnits();
                
                if (Battle.Instance.IsEventBlockingAI)
                {
                    yield return Battle.Instance.WaitForEventBlocking();
                }
            }
            Debug.Log("[AI] End Turn.");
        }

        private IEnumerator ExecuteUnitAction(Battalion unit)
        {
            // 1. 获取基础攻击
            ActionDefinition attackAction = unit.DefaultAttack;
            if (attackAction == null)
            {
                Debug.LogWarning($"[AI] Unit {unit.name} has no DefaultAttack!");
                unit.actionDone = true;
                yield break;
            }

            // 2. 寻找最近的玩家单位
            Unit target = Battle.Instance.FindNearestUnit(unit, Faction.Player);
            
            if (target == null)
            {
                Debug.Log($"[AI] Unit {unit.name} found NO target.");
                unit.actionDone = true;
                yield break;
            }

            int dist = Battle.Instance.GetHexDistance(unit.position, target.position);
            Debug.Log($"[AI] Unit {unit.name} found target: {target.name} at {target.position}. Distance: {dist}. Attack Range: {attackAction.range}");

            // 3. 决策：移动 + 攻击
            bool canAttackNow = dist <= attackAction.range && dist >= attackAction.minRange;

            if (canAttackNow)
            {
                Debug.Log($"[AI] Target in range. Attacking directly.");
                PerformAttack(unit, attackAction, target);
            }
            else
            {
                // 需要移动
                Debug.Log($"[AI] Target out of range. Calculating best move position...");
                Vector2Int bestPos = FindBestPosition(unit, target, attackAction);
                
                Debug.Log($"[AI] Best move position calculated: {bestPos}. Current pos: {unit.position}");

                if (bestPos != unit.position)
                {
                    // 生成路径
                    List<Vector2Int> path = Battle.Instance.FindPath(unit.position, bestPos, unit);
                    
                    if (path != null && path.Count > 0)
                    {
                        Debug.Log($"[AI] Path found with {path.Count} steps. Moving...");
                        // 执行平滑移动
                        yield return StartCoroutine(Battle.Instance.MoveUnit(unit, path));
                        
                        // 移动后再次检查攻击
                        int newDist = Battle.Instance.GetHexDistance(unit.position, target.position);
                        if (newDist <= attackAction.range && newDist >= attackAction.minRange)
                        {
                            Debug.Log($"[AI] Moved and now in range ({newDist}). Attacking.");
                            yield return new WaitForSeconds(0.3f); // 稍微停顿
                            PerformAttack(unit, attackAction, target);
                        }
                        else
                        {
                            Debug.Log($"[AI] Moved but still out of range ({newDist}). End turn.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AI] FindPath returned null or empty path to {bestPos}!");
                    }
                }
                else
                {
                    Debug.Log($"[AI] Unit decided to stay at {unit.position}. (Maybe blocked or already best pos)");
                }
            }
            
            unit.actionDone = true;
        }

        private Vector2Int FindBestPosition(Battalion unit, Unit target, ActionDefinition action)
        {
            // 获取所有可移动位置 (基于当前移动力)
            HashSet<Vector2Int> moveableTiles = Battle.Instance.GetAccessableTilesInRange(unit, unit.currentMovement);
            
            Debug.Log($"[AI] Moveable tiles count: {moveableTiles.Count}. Movement: {unit.currentMovement}");

            Vector2Int bestPos = unit.position;
            
            bool foundAttackPos = false;
            int minDistanceToTarget = int.MaxValue;

            // 初始距离 (用于比较是否值得移动)
            int currentDist = Battle.Instance.GetHexDistance(unit.position, target.position);
            minDistanceToTarget = currentDist;

            foreach (var pos in moveableTiles)
            {
                // 排除不可停留的位置 (除了自己当前位置)
                if (pos != unit.position && !Battle.Instance.CanUnitStopOnTile(unit, pos)) continue;

                int distToTarget = Battle.Instance.GetHexDistance(pos, target.position);
                bool canAttack = distToTarget <= action.range && distToTarget >= action.minRange;

                // Debug.Log($"[AI] Checking tile {pos}. Dist to target: {distToTarget}. Can attack: {canAttack}");

                if (canAttack)
                {
                    // 如果找到了能攻击的位置
                    if (!foundAttackPos)
                    {
                        bestPos = pos;
                        minDistanceToTarget = distToTarget; 
                        foundAttackPos = true;
                        // Debug.Log($"[AI] -> Found FIRST attack pos: {pos}");
                    }
                    else
                    {
                        // 已经有攻击位了，比较谁更好 (贴脸打)
                        if (distToTarget < minDistanceToTarget)
                        {
                            bestPos = pos;
                            minDistanceToTarget = distToTarget;
                            // Debug.Log($"[AI] -> Found BETTER attack pos: {pos} (Closer)");
                        }
                    }
                }
                else if (!foundAttackPos)
                {
                    // 还没找到攻击位，只能找个离目标最近的格子赶路
                    if (distToTarget < minDistanceToTarget)
                    {
                        bestPos = pos;
                        minDistanceToTarget = distToTarget;
                        // Debug.Log($"[AI] -> Found CLOSER move pos: {pos} (Dist: {distToTarget})");
                    }
                }
            }
            
            return bestPos;
        }

        private void PerformAttack(Battalion source, ActionDefinition action, Unit target)
        {
            Debug.Log($"[AI] {source.name} performing attack on {target.name}");
            
            // 执行攻击
            bool success = action.Perform(source, target.position);
            
            if (success)
            {
                if (Battle.Instance != null) Battle.Instance.ResolveDirtyUnits();
            }
            else
            {
                Debug.LogError($"[AI] Attack failed! Perform returned false.");
            }
        }
    }
}