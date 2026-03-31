using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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
                
                if (unit == null) continue;
                if (unit is not Battalion aiBattalion) continue;
                if (aiBattalion.actionDone) continue;
                
                float thinkDelay = Battle.Instance.IsUnitVisibleToPlayer(unit) ? BattleParam.Instance.enemyThinkingDelay : 0f;
                List<Unit> targetsInVision = Battle.Instance.FindVisibleOpponentsInVision(unit);
                // 模拟思考时间
                if (thinkDelay > 0) yield return new WaitForSeconds(thinkDelay);

                if (targetsInVision.Count > 0)
                {
                    // **攻击模式**
                    Debug.Log($"[AI] {unit.name} 发现目标，进入攻击模式。");
                    yield return StartCoroutine(AttackRoutine(unit, targetsInVision));
                }
                else
                {
                    // **巡逻模式**
                    Debug.Log($"[AI] {unit.name} 未发现目标，进入巡逻模式。");
                    yield return StartCoroutine(PatrolRoutine(unit));
                }
                
                if (thinkDelay > 0) yield return new WaitForSeconds(thinkDelay);
                
                if (Battle.Instance != null) Battle.Instance.ResolveDirtyUnits();
                
                if (Battle.Instance.IsEventBlockingAI)
                {
                    yield return Battle.Instance.WaitForEventBlocking();
                }
            }
            Debug.Log("[AI] End Turn.");
        }
        private Unit FindBestTarget(Battalion source, HashSet<Unit> visibleTargets)
        {
            Unit nearestTarget = null;
            int minDistance = int.MaxValue;

            if (visibleTargets == null || visibleTargets.Count == 0)
            {
                Debug.Log($"[{source.name}] No visible targets.");
                return null;
            }

            foreach (var target in visibleTargets)
            {
                if (target == null || target.currentHealth <= 0) continue;

                int distance = Battle.Instance.GetHexDistance(source.position, target.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTarget = target;
                }
            }
    
            if (nearestTarget != null)
            {
                Debug.Log($"[{source.name}] Nearest Target: {nearestTarget.name} at distance {minDistance}");
            }
    
            return nearestTarget;
        }
        private IEnumerator AttackRoutine(Unit aiUnit, List<Unit> targets)
        {
            // 1. 选择最近的目标
            Unit nearestTarget = targets.OrderBy(t => Battle.Instance.GetHexDistance(aiUnit.position, t.position)).FirstOrDefault();
            if (nearestTarget == null) yield break;

            // 2. 检查是否可以直接攻击
            ActionDefinition attackAction = aiUnit.DefaultAttack;
            if (attackAction != null && attackAction.IsTileValidTarget(aiUnit, nearestTarget.position))
            {
                Debug.Log($"[AI] {aiUnit.name} 直接攻击 {nearestTarget.name}");
                yield return StartCoroutine(Battle.Instance.ExecuteActionCoroutine(aiUnit, nearestTarget.position, attackAction));
                yield break;
            }

            Vector2Int moveTargetPos = FindBestAttackPosition(aiUnit, nearestTarget, attackAction);

            if (Battle.Instance.IsValidMapPosition(moveTargetPos))
            {
                List<Vector2Int> path = Battle.Instance.FindPath(aiUnit.position, moveTargetPos, aiUnit, true);

                if (path != null && path.Count > 0)
                {
                    Debug.Log($"[AI] {aiUnit.name} 移动到 {moveTargetPos} 以攻击 {nearestTarget.name}");
                    
                    Battalion bat = aiUnit as Battalion;
                    if (bat != null) bat.CurrentSoldierState = SoldierState.Move;
                    
                    bool moveInterrupted = false;
                    yield return StartCoroutine(Battle.Instance.MoveUnit(aiUnit, path, wasInterrupted => {
                        moveInterrupted = wasInterrupted;
                    }));
                    
                    if (bat != null) bat.CurrentSoldierState = SoldierState.Idle;
                    
                    Battle.Instance.RefreshAllZOCAndVision(aiUnit);
                    if (moveInterrupted)
                    {
                        Debug.Log($"[AI] {aiUnit.name} 的移动被打断！");
                        
                        yield break; // 移动被打断，结束回合
                    }
                    

                    // 移动后再次检查是否可以攻击
                    if (attackAction != null && attackAction.IsTileValidTarget(aiUnit, nearestTarget.position))
                    {
                        Debug.Log($"[AI] {aiUnit.name} 移动后攻击 {nearestTarget.name}");
                        yield return StartCoroutine(Battle.Instance.ExecuteActionCoroutine(aiUnit, nearestTarget.position, attackAction));
                    }
                }
            }
        }
        
        private IEnumerator PatrolRoutine(Unit aiUnit)
        {
            // 1. 获取视野内的可移动格子
            var tilesInVision = Battle.Instance.GetAllTilesInRange(aiUnit.position, aiUnit.GetVisionRange());
            var stoppableTiles = tilesInVision.Where(p => Battle.Instance.CanUnitStopOnTile(aiUnit, p, true)).ToList();

            if (stoppableTiles.Count > 0)
            {
                // 2. 随机选择一个目标点
                Vector2Int patrolTarget = stoppableTiles[Random.Range(0, stoppableTiles.Count)];

                // 3. 移动到该点
                List<Vector2Int> path = Battle.Instance.FindPath(aiUnit.position, patrolTarget, aiUnit, true);
                if (path != null && path.Count > 0)
                {
                    Debug.Log($"[AI] {aiUnit.name} 巡逻到 {patrolTarget}");
                    Battalion bat = aiUnit as Battalion;
                    if (bat != null) bat.CurrentSoldierState = SoldierState.Move;

                    yield return StartCoroutine(Battle.Instance.MoveUnit(aiUnit, path, wasInterrupted =>
                    {
                        if (wasInterrupted) Debug.Log($"[AI] {aiUnit.name} 的巡逻移动被打断！");


                    }));
                    if (bat != null) bat.CurrentSoldierState = SoldierState.Idle;
                }
                Battle.Instance.RefreshAllZOCAndVision(aiUnit);
            }
            // 如果没有可移动的点，则原地待命
            aiUnit.actionDone = true;
        }
        
        private Vector2Int FindBestAttackPosition(Unit aiUnit, Unit target, ActionDefinition action)
        {
            Vector2Int bestPos = -Vector2Int.one;
            int minDist = int.MaxValue;

            // 获取目标周围可以攻击的格子
            var potentialAttackPositions = Battle.Instance.GetAllTilesInRange(target.position, action.maxRange);

            foreach (var pos in potentialAttackPositions)
            {
                // 检查这个格子是否是有效的攻击发起点
                if (action.IsTileValidTarget(aiUnit, target.position, pos))
                {
                    // 检查这个格子是否是 AI 可以移动到的地方
                    if (Battle.Instance.CanUnitStopOnTile(aiUnit, pos, true))
                    {
                        int dist = Battle.Instance.GetHexDistance(aiUnit.position, pos);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestPos = pos;
                        }
                    }
                }
            }
            return bestPos;
        }
        
    }
}