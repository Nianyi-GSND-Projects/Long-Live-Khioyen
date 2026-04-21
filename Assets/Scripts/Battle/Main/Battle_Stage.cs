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
        public event System.Action BattleStart;
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
                case Stage.Preparation:
                    Debug.Log("OnEnter: 准备阶段");
                    UpdatePlayerVisionSources();
                    UpdateFogOfWar();
                    BattleStart?.Invoke();
                    HighlightTilesRing(availableArrangementPositions, deployRingColor);
                    Battle.Instance.RefreshFogOfWar(true); 
                    break;
                case Stage.Arrangement:
                    Debug.Log("OnEnter: 布置阶段");
                    ClearAllSelection();
                    UpdatePlayerVisionSources();
                    UpdateFogOfWar();
                    HighlightTilesRing(availableArrangementPositions, deployRingColor);
                    break;
                case Stage.Battle:
                    RefreshAllZOC();
                    UpdatePlayerVisionSources();
                    UpdateFogOfWar();
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
                    ClearAllHexRingHighlights();
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
                OnPlayerTurnEnded?.Invoke();
                Debug.Log("Updating Player & Friend units...");
                var playerunits = new List<Unit>(factionActiveUnits[Faction.Player]);
                foreach (var unit in playerunits)
                {
                    unit.OnTurnEnd();
                }
                var friendunits = new List<Unit>(factionActiveUnits[Faction.Friend]);
                foreach (var unit in friendunits)
                {
                    unit.OnTurnEnd();
                }
                ResolveDirtyUnits();
                
                yield return new WaitForSeconds(0.5f);
                CurrentTurnState = TurnState.EnemyTurn;
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnEnemyTurnStart);
                yield return StartCoroutine(EnemyTurnCoroutine());
				
                if (BattleEventManager.Instance != null)
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnEnemyTurnEnd);
                CheckBattleEnd();
                if (CurrentStage == Stage.Settlement) yield break;
				
                CurrentTurnState = TurnState.Processing;
                
                Debug.Log("Updating Enemy units buffs...");
                var units = new List<Unit>(factionActiveUnits[Faction.Enemy]);
                foreach (var unit in units)
                {
                    unit.OnTurnEnd();;
                }
                ResolveDirtyUnits();
                yield return new WaitForSeconds(0.5f);
				
                UpdateAllTileEffects(); 
                ResolveDirtyUnits();
            }

        }
        
        #endregion

        #region PlayerTurnStage
        
        private Vector2Int initialUnitPosition;
        private int initialUnitMovement;
        public bool IsOperatingUnit { get; set; } = false;
        public PlayerActionStage CurrentActionStage{ get; set; }
        private PlayerActionStage _previousActionStage;
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
            IsOperatingUnit = false;
            ChangeActionStage(PlayerActionStage.None);
            foreach (var unit in factionActiveUnits[Faction.Player])
            {
                unit.selected = false;
            }
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
                    //CancelMovement();
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.MovingBattalion);
                }
                if (CurrentActionStage == PlayerActionStage.MovingBattalion)
                {
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.None);
                }

                IsOperatingUnit = false;
                ClearAllHexRingHighlights();
                IsPlayerTurnOver = true;
            }
            else Debug.LogError("It's not player's turn!");
        }
        
        public void CancelAction()
        {
            if (SelectedUnit is Battalion bat) bat.CurrentSoldierState = SoldierState.Idle;
            availableTargetPositions.Clear();
            CurrentAction = null;
            IsPreparingAction = false;
            ClearAllHexRingHighlights();
        }
        
        public FacilityDefinition PendingFacility { get; set; }
        
        public Unit BuildPendingFacility(Vector2Int pos, Faction faction)
        {
            if (PendingFacility == null)
            {
                Debug.LogWarning("No PendingFacility to build!");
                return null;
            }

            FacilityDescriptor desc = new FacilityDescriptor
            {
                Definition = PendingFacility,
                faction = faction,
                isVisible = PendingFacility.defaultVisibility,
                zocPower = PendingFacility.defaultZocPower,
                visionRange = PendingFacility.defaultVisionRange,
                instanceId = -1,
                maxDurability = PendingFacility.defaultMaxDurability,
                currentDurability = 1,
                isConstructed = false// 初始 1 血
            };

            return RegisterUnitToBattle(desc, pos);
        }
        public event System.Action<PlayerActionStage> OnActionStageChanged;
        public void ChangeActionStage(PlayerActionStage stage)
        {
            
            if (stage == PlayerActionStage.SelectingTarget)
            {
                if (SelectedUnit is Battalion bat) bat.CurrentSoldierState = SoldierState.Idle;
                _previousActionStage = CurrentActionStage;
            }
            
            if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget)
            {
                SetCameraLocked(false);
                OnAmbiguousSelectionEnded?.Invoke();
                currentAmbiguousCandidates = null;
            }
			
            if (CurrentActionStage == PlayerActionStage.SelectingAction)
            {
                OnActionSelectionEnded?.Invoke();
            }
            
            ClearAllHexRingHighlights();
            CurrentActionStage = stage;
            switch (stage)
            {
                case PlayerActionStage.None:
                    Debug.Log("Change action stage to None");
                    ClearAllSelection();
                    ClearAllHexRingHighlights();
                    break;
				
                case PlayerActionStage.MovingBattalion:
                    Debug.Log("Change action stage to MovingBattalion");
                    ClearAllHexRingHighlights();
                    HighlightTilesRing(availableMovePositions, moveRingColor);
                    break;
				
                case PlayerActionStage.SelectingAction:
                    Debug.Log("Change action stage to SelectingAction");
                    RefreshAllZOCAndVision(SelectedUnit);
                    ClearAllHexRingHighlights();
                    OnActionSelectionStarted?.Invoke();
                    break;
                
                case PlayerActionStage.SelectingSubAction:
                    // UI 监听此状态 -> 显示二级菜单
                    break;
        
                case PlayerActionStage.SelectingBuildItem:
                    // UI 监听此状态 -> 显示建造面板
                    break;
				
                case PlayerActionStage.SelectingTarget:
                    if (CurrentAction != null)
                    {
                        if (SelectedUnit is Battalion bat)
                        {
                            bat.CurrentSoldierState = CurrentAction.targetingVisualState;
                        }
                        
                        availableTargetPositions = GetValidActionTargetTiles(SelectedUnit, CurrentAction);
                
                        Color targetColor = targetNeutralColor;
                        
                        if (CurrentAction.targetFactionType==TargetFactionType.Enemy)
                            targetColor = targetEnemyColor;
                        else if (CurrentAction.targetFactionType==TargetFactionType.Friend||CurrentAction.targetCountType==TargetCountType.Self)
                            targetColor = targetFriendColor;
                
                        HighlightTilesOverlay(availableTargetPositions, targetColor);
                    }
                    else
                    {
                        Debug.LogError("进入选择目标阶段，但 CurrentAction 为空！");
                        ChangeActionStage(PlayerActionStage.SelectingAction);
                    }
                    break;
				
                case PlayerActionStage.SelectingAmbiguousTarget:
                    Debug.Log("Change action stage to SelectingAmbiguousTarget");
                    SetCameraLocked(true);
                    ClearAllHexRingHighlights();
                    // 触发事件，把刚才存下来的列表发给 UI
                    OnAmbiguousSelectionStarted?.Invoke(currentAmbiguousCandidates);
                    break;
            }
            OnActionStageChanged?.Invoke(stage);
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
            ExecuteActionLogic(source, targetPos, CurrentAction);
        }
        
        public void ExecuteActionLogic(Unit source, Vector2Int targetPos, ActionDefinition actionToPerform)
        {
            if (actionToPerform == null) return;
            
            // 启动改写后的行动序列协程
            StartCoroutine(ExecuteActionCoroutine(source, targetPos, actionToPerform));
        }
        
        public IEnumerator ExecuteActionCoroutine(Unit source, Vector2Int targetPos, ActionDefinition actionToPerform)
        {
            HighlightTilesOverlay(availableTargetPositions, Color.clear);
            // 等待整个行动（包括所有 Effect 及其动画）执行完毕
            yield return StartCoroutine(actionToPerform.PerformRoutine(source, targetPos));

            ResolveDirtyUnits();
            if (source != null)
            {
                source.actionDone = true;
            }
            IsOperatingUnit = false;
            ClearAllSelection();
            ChangeActionStage(PlayerActionStage.None);
            
            if (BattleEventManager.Instance != null)
                BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnUnitActionEnd, source);
        }

        public int CalculatePathCost(List<Vector2Int> path, Unit unit)
        {
            if (path == null || path.Count == 0) return 0;
            
            int totalCost = 0;
            foreach (var pos in path)
            {
                totalCost += (1 + CalculateExtraMoveCost(unit, pos));
            }
            return totalCost;
        }
        
        private IEnumerator PerformPlayerMove(Unit unit, Vector2Int targetPos)
        {
            IsUnitMoving = true;
            Battalion bat = unit as Battalion;
            
            
            if (targetPos == unit.position)
            {
                if (bat != null)
                {
                    bat.CurrentSoldierState = SoldierState.Idle;
                }
                ChangeActionStage(PlayerActionStage.SelectingAction);
                IsUnitMoving = false;
                yield break;
            }
            
            
            
            List<Vector2Int> path = FindPath(unit.position, targetPos, unit, true);
	
            if (path != null && path.Count > 0)
            {
                Vector2Int startPos = unit.position;
                bool moveInterrupted = false;
                if (bat != null)
                {
                    bat.CurrentSoldierState = SoldierState.Move;
                }
                yield return StartCoroutine(MoveUnit(unit, path, wasInterrupted => {
                    moveInterrupted = wasInterrupted;
                }));

                // 移动结束后统一结算所有脏单位（移动途中踩陷阱等产生的伤亡）
                ResolveDirtyUnits();
                
                // 单位在移动中死亡（如踩陷阱），直接清理状态并返回
                if (unit == null || !unit.gameObject.activeSelf||!unit.gameObject)
                {
                    IsOperatingUnit = false;
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.None);
                    IsUnitMoving = false;
                    yield break;
                }

                if (bat != null)
                {
                    bat.CurrentSoldierState = SoldierState.Idle;
                }

                if (unit != null && (unit.faction == Faction.Player || unit.faction == Faction.Friend))
                {
                    UpdatePlayerVisionSources();
                    UpdateFogOfWar();
                }
		
                if (targetPos != initialUnitPosition)
                {
                    unit.hasMovedThisTurn = true;
                }
                
                int realMoveCost = CalculatePathCost(path, unit);
                if(unit is Battalion batAfterMove)
                    batAfterMove.currentMovement -= realMoveCost;
                
                if (moveInterrupted)
                {
                    // 如果被打断，直接结束移动，进入行动选择
                    Debug.Log("移动被打断，本回合无法继续移动。");
                    ChangeActionStage(PlayerActionStage.SelectingAction);
                }
                else if (unit is Battalion batRemainMove && batRemainMove.currentMovement > 0)
                {
                    // 正常完成且还有移动力，刷新范围
                    UpdateAvailableMovePositions(batRemainMove);
                }
                else
                {
                    // 正常完成但移动力耗尽，进入行动选择
                    ChangeActionStage(PlayerActionStage.SelectingAction);
                }
                
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
            IsOperatingUnit = false;
            ClearAllSelection();
            ChangeActionStage(PlayerActionStage.None);
        }
        
        #endregion
    }
}
