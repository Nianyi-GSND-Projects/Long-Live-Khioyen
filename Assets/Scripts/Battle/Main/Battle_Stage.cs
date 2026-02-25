using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
        #region BattleStage
        public Stage CurrentStage{ get; set; }
        public event System.Action OnStageChanged;
		
        public bool IsInArrangementStage { get; set; } = false;
        public bool IsInBattleStage { get; set; }= false;
        public bool IsReserveTeamSelected { get; set; }= false;
        public bool IsUnitSelected { get; set; }= false;
        public void ProceedToNextStage()
        {
            switch(CurrentStage)
            {
                case Stage.Preparation:
                    ChangeStage(Stage.Arrangement);
                    break;
                case Stage.Arrangement:
                    ChangeStage(Stage.Battle);
                    break;
                case Stage.Battle:
                    ChangeStage(Stage.Settlement);
                    break;
                default:
                    break;
            }
        }
        public void ChangeStage(Stage stage)
        {
            OnExitStage(CurrentStage);
			
            CurrentStage = stage;
            OnEnterStage(CurrentStage);
            OnStageChanged?.Invoke();
        }
        
        void OnEnterStage(Stage stage)
        {
            switch (stage)
            {
                case Stage.Arrangement:
                    Debug.Log("OnEnter: 布置阶段");
                    HighlightTiles(availableArrangementPositions,arrangementHighlightColor);
                    break;
                case Stage.Battle:
                    battleLoopCoroutine = StartCoroutine(BattleTurnLoop());
                    Debug.Log("OnEnter: 战斗阶段");
                    break;
                case Stage.Settlement:
                    Debug.Log("OnEnter: 结算阶段");
                    break;
            }
        }
		
        void OnExitStage(Stage stage)
        {
            switch (stage)
            {
                case Stage.Arrangement:
                    Debug.Log("OnExit: 布置阶段");
                    ClearAllHexHighlights();
                    ClearAllSelection();
                    break;
                case Stage.Battle:
                    if (battleLoopCoroutine != null)
                    {
                        StopCoroutine(battleLoopCoroutine);
                        battleLoopCoroutine = null;
                    }
                    Debug.Log("OnExit: 战斗阶段");
                    break;
            }
        }

        #endregion
        
        #region Turn Control
        
        public event System.Action OnPlayerTurnStarted;
        public event System.Action OnPlayerTurnEnded;
        public event System.Action OnActionSelectionStarted;
        public event System.Action OnActionSelectionEnded;
        
        public int TurnCount { get; private set; }
        private Coroutine battleLoopCoroutine;
        
        public bool IsPlayerTurnOver { get; set; }
		
        public TurnState CurrentTurnState{ get; set; }
        
        private IEnumerator BattleTurnLoop()
        {
            Debug.Log("Battle Start!");
            while (true)
            {
                CurrentTurnState = TurnState.PlayerTurn;
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnPlayerTurnStart);
				
                yield return StartCoroutine(PlayerTurnCoroutine());
				
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnPlayerTurnEnd);
				
                CheckBattleEnd();
                if (CurrentStage == Stage.Settlement) yield break;
				
                CurrentTurnState = TurnState.Processing;
                yield return new WaitForSeconds(1);
                
                CurrentTurnState = TurnState.EnemyTurn;
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnEnemyTurnStart);
                yield return StartCoroutine(EnemyTurnCoroutine());
				
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnEnemyTurnEnd);
                CheckBattleEnd();
                if (CurrentStage == Stage.Settlement) yield break;
				
                CurrentTurnState = TurnState.Processing;
                yield return new WaitForSeconds(1);
				
                UpdateAllTileEffects(); 
                //UpdateAllUnitBuffs(); 
            }

        }
        
        #endregion

        #region PlayerTurnStage
        
        private Vector2Int initialUnitPosition;
        private int initialUnitMovement;
        public PlayerActionStage CurrentActionStage{ get; set; }
        private IEnumerator PlayerTurnCoroutine()
        {
            IsPlayerTurnOver = false;
            TurnCount++;
            Debug.Log("Player Turn!");

            foreach (var unit in factionActiveUnits[Faction.Player])
            {
                unit.OnTurnStart();
            }
            //
            OnPlayerTurnStarted?.Invoke();

            while (!IsPlayerTurnOver)
            {
                yield return null;
            }
            Debug.Log("Player Turn End!");
            ChangeActionStage(PlayerActionStage.None);
            foreach (var unit in factionActiveUnits[Faction.Player])
            {
                //改成实际数值
                unit.selected = false;
            }
            OnPlayerTurnEnded?.Invoke();
        }
        
        public void EndPlayerTurn()
        {
            if (CurrentTurnState == TurnState.PlayerTurn)
            {
				
                if (CurrentActionStage == PlayerActionStage.SelectingTarget)
                {
                    CancelAction();
                    ChangeActionStage(PlayerActionStage.SelectingAction);
                }
                if (CurrentActionStage == PlayerActionStage.SelectingAction)
                {
                    CancelMovement();
                    ChangeActionStage(PlayerActionStage.MovingBattalion);
                }
                if (CurrentActionStage == PlayerActionStage.MovingBattalion)
                {
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.None);
                }
                ClearAllHexHighlights();
                IsPlayerTurnOver = true;
            }
            else Debug.LogError("It's not player's turn!");
        }
        
        public void CancelMovement()
        {
            RemoveUnitFromMap(SelectedUnit);
            SelectedUnit.position = initialUnitPosition;
            PlaceUnitOnMap( SelectedUnit,initialUnitPosition);
            if(SelectedUnit is Battalion bat) bat.currentMovement = initialUnitMovement;
            SelectedUnit.transform.localPosition = MapToLocal(initialUnitPosition);
            SelectedUnit.hasMovedThisTurn = false;
            availableMovePositions = GetAccessableTilesInRange(SelectedUnit, initialUnitMovement);
        }
        
        public void CancelAction()
        {
            availableTargetPositions.Clear();
            CurrentAction = null;
            IsPreparingAction = false;
            ClearAllHexHighlights();
        }
        public void ChangeActionStage(PlayerActionStage stage)
        {
            if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget)
            {
                OnAmbiguousSelectionEnded?.Invoke();
                currentAmbiguousCandidates = null;
            }
			
            if (CurrentActionStage == PlayerActionStage.SelectingAction)
            {
                OnActionSelectionEnded?.Invoke();
            }
			
            CurrentActionStage = stage;
            switch (stage)
            {
                case PlayerActionStage.None:
                    Debug.Log("Change action stage to None");
                    ClearAllSelection();
                    ClearAllHexHighlights();
                    break;
				
                case PlayerActionStage.MovingBattalion:
                    Debug.Log("Change action stage to MovingBattalion");
                    ClearAllHexHighlights();
                    HighlightTiles(availableMovePositions,movementHighlightColor);
                    break;
				
                case PlayerActionStage.SelectingAction:
                    Debug.Log("Change action stage to SelectingAction");
                    ClearAllHexHighlights();
                    OnActionSelectionStarted?.Invoke();
                    //TODO:单位处悬浮菜单，锁定滚动
                    break;
				
                case PlayerActionStage.SelectingTarget:
                    if (CurrentAction != null)
                    {
                        availableTargetPositions = GetValidActionTargetTiles(SelectedUnit, CurrentAction);
                        HighlightTiles(availableTargetPositions, attackHighlightColor); // 建议改个名，比如 targetHighlightColor
                        Debug.Log($"进入目标选择阶段: {CurrentAction.actionName}, 可选目标数: {availableTargetPositions.Count}");
                    }
                    else
                    {
                        Debug.LogError("进入选择目标阶段，但 CurrentAction 为空！");
                        ChangeActionStage(PlayerActionStage.SelectingAction);
                    }
                    break;
				
                case PlayerActionStage.SelectingAmbiguousTarget:
                    Debug.Log("Change action stage to SelectingAmbiguousTarget");
                    ClearAllHexHighlights();
                    // 触发事件，把刚才存下来的列表发给 UI
                    OnAmbiguousSelectionStarted?.Invoke(currentAmbiguousCandidates);
                    break;
            }
        }
        #endregion

        #region AITurn

        private IEnumerator EnemyTurnCoroutine()
        {
            Debug.Log("Enemy Turn!");
            yield return new WaitForSeconds(1.0f); 
			
			
            // 获取所有敌方单位
            List<Unit> enemyUnits = new List<Unit>(factionActiveUnits[Faction.Enemy]);
			
            foreach (var unit in enemyUnits)
            {
                if (unit != null && unit.gameObject.activeSelf)
                {
                    unit.OnTurnStart();
                }
            }
				
            // 确保 AIController 存在
            if (AIController.Instance == null)
            {
                // 如果场景里没挂，临时挂一个
                gameObject.AddComponent<AIController>();
            }

            // 交给 AIController 处理
            yield return StartCoroutine(AIController.Instance.ProcessTurn(enemyUnits));
			
            Debug.Log("Enemy Turn End!");
			
            // 重置状态
            foreach (var unit in factionActiveUnits[Faction.Enemy])
            {
                unit.actionDone = false;
            }
        }

        #endregion

        #region PlayerAction

        public bool IsPreparingAction{ get; set; }
        
        public ActionDefinition CurrentAction { get; private set; }
        
        public void PrepareAction(ActionDefinition action)
        {
            if (action == null) return;

            IsPreparingAction = true;
            CurrentAction = action;
			
            ChangeActionStage(PlayerActionStage.SelectingTarget);
        }
		
        public void ApplyAction(Vector2Int mapPosition)
        {
            if (!IsUnitSelected || CurrentAction == null)
            {
                Debug.LogWarning("No unit selected or no action prepared.");
                return;
            }

            if (!CurrentAction.IsTileValidTarget(SelectedUnit, mapPosition))
            {
                Debug.Log($"位置 {mapPosition} 无效。");
                return;
            }
			
            ExecuteActionLogic(SelectedUnit, mapPosition);
			
        }
        
        private void ExecuteActionLogic(Unit source, Vector2Int targetPos)
        {

            bool success = CurrentAction.Perform(source, targetPos);

            if (success)
            {
                ResolveDirtyUnits();

                if (SelectedUnit) SelectedUnit.actionDone = true;
                ClearAllSelection();
                ChangeActionStage(PlayerActionStage.None);
				
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnUnitActionEnd,source);

            }
        }
        
        private IEnumerator PerformPlayerMove(Unit unit, Vector2Int targetPos)
        {
            IsUnitMoving = true;
			
            if (targetPos == unit.position)
            {
                // 直接进入行动选择阶段
                ChangeActionStage(PlayerActionStage.SelectingAction);
                IsUnitMoving = false;
                yield break;
            }
            // 1. 计算路径
            // 注意：FindPath 需要在 Battle.cs 中实现 (之前为 AI 加的那个)
            List<Vector2Int> path = FindPath(unit.position, targetPos, unit);
	
            if (path != null && path.Count > 0)
            {
                // 2. 执行移动动画
                yield return StartCoroutine(MoveUnit(unit, path));
		
                // 3. 移动后逻辑
                if (targetPos != initialUnitPosition) // 检查是否真的动了
                {
                    unit.hasMovedThisTurn = true;
                }
		
                // TODO: 移动实际减少移动力 (目前简化为不减，或者在 MoveUnit 里减)
                // unit.currentMovement -= path.Count; 

                ChangeActionStage(PlayerActionStage.SelectingAction);
            }
            else
            {
                Debug.LogError($"无法找到通往 {targetPos} 的路径！");
            }

            IsUnitMoving = false;
        }
        
        public void ActionWait()
        {
            SelectedUnit.actionDone = true;
            ClearAllSelection();
            ChangeActionStage(PlayerActionStage.None);
        }
        
        #endregion
    }
}
