using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
        // 统一入口：处理格子点击
        public void HandleGridInput(Vector2Int gridPos)
        {
            if (!IsValidMapPosition(gridPos)) return;

            // 1. 移动中禁止操作
            if (IsUnitMoving) return;

            // 2. 歧义选择中禁止操作 (除非点击了 UI，但 UI 会拦截)
            if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget) return;

            // 3. 根据阶段分发
            switch (CurrentStage)
            {
                case Stage.Arrangement:
                    HandleArrangementInput(gridPos);
                    break;
                
                case Stage.Battle:
                    HandleBattleInput(gridPos);
                    break;
            }
        }
        
        public void SetInputBlocked(bool blocked)
        {
            var input = GetComponent<BattleInputController>();
            if (input != null)
            {
                input.inputBlocked = blocked;
            }
        
            if (BattleUi.Instance != null)
            {
                // BattleUi.Instance.SetProceedUIInteractable(!blocked); // 需要在 BattleUi 加这个方法
            }
        }

        // 统一入口：处理取消/右键
        public void HandleCancelInput()
        {
            if (CurrentStage == Stage.Arrangement)
            {
                ClearAllSelection();
            }
            else if (CurrentStage == Stage.Battle)
            {
                // 尝试让 UI 处理回退 (比如关闭子菜单)
                if (BattleUi.Instance != null && BattleUi.Instance.TryHandleBackInput()) return;

                // 否则执行游戏逻辑回退
                if (CurrentActionStage == PlayerActionStage.SelectingTarget)
                {
                    CancelAction();
                    ChangeActionStage(PlayerActionStage.SelectingAction);
                }
                else if (CurrentActionStage == PlayerActionStage.SelectingAction)
                {
                    CancelMovement();
                    ChangeActionStage(PlayerActionStage.MovingBattalion);
                }
                else if (CurrentActionStage == PlayerActionStage.MovingBattalion)
                {
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.None);
                }
                else
                {
                    ClearAllSelection();
                }
            }
        }


        private void HandleArrangementInput(Vector2Int gridPos)
        {
            if (IsReserveTeamSelected)
            {
                // 放置预备队
                if (SelectedBattalionDescriptor.placed) return;
                if (!ValidateArrangementPlacement(gridPos)) return;
                
                PlacingPlayerBattalion(SelectedBattalionDescriptor, gridPos);
            }
            else if (IsUnitSelected)
            {
                // 移动已部署单位
                if (SelectedUnit is not Battalion) return;
                if (!ValidateArrangementPlacement(gridPos)) return;
                
                MovingBattalion(gridPos); // Arrangement 模式下是瞬移
            }
            else
            {
                // 选中单位
                TrySelectUnitAt(gridPos);
            }
        }

        private void HandleBattleInput(Vector2Int gridPos)
        {
            InteractWithTile(gridPos);
        }
        
        private void TrySelectUnitAt(Vector2Int gridPos)
        {
            TileData tile = mapData[gridPos.x, gridPos.y];
    
            if (tile.Battalion != null)
            {
                SelectUnit(tile.Battalion);
            }
            else if (tile.Facility != null)
            {
                SelectUnit(tile.Facility);
            }
            else
            {
                ClearAllSelection();
            }
        }

        #region Logic

        public void InteractWithTile(Vector2Int gridPos)
        {
            if (!IsValidMapPosition(gridPos)) return;
            if (IsUnitMoving) return;
            if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget) 
                return;
			
            if (CurrentStage == Stage.Battle && CurrentTurnState != TurnState.PlayerTurn)
            {
                Debug.Log("Not your turn!");
                return;
            }
			
            if (CurrentActionStage == PlayerActionStage.SelectingTarget)
            {
                if (availableTargetPositions.Contains(gridPos))
                {
                    ApplyAction(gridPos);
                }
                else
                {
                    // 点击了无效目标 -> 取消行动选择，回退到菜单
                    //CancelAction();
                }
                return;
            }
			
            if (CurrentActionStage == PlayerActionStage.SelectingAction)
            {
                CancelMovement();
                ChangeActionStage(PlayerActionStage.MovingBattalion);
                return;
            }
			
            if (CurrentActionStage == PlayerActionStage.MovingBattalion)
            {
                if (availableMovePositions.Contains(gridPos))
                {
                    MovingBattalion(gridPos);
                }
                else
                {
                    ClearAllSelection();
                    ChangeActionStage(PlayerActionStage.None);
                }
                return;
            }

            if (CurrentActionStage == PlayerActionStage.None)
            {
                TileData tile = mapData[gridPos.x, gridPos.y];
                List<Unit> candidates = new List<Unit>();
                if (tile.Battalion != null) candidates.Add(tile.Battalion);
                if (tile.Facility != null) candidates.Add(tile.Facility);

                if (candidates.Count == 0)
                {
                    if (CurrentActionStage == PlayerActionStage.None)
                    {
                        ClearAllSelection();
                    }

                    return;
                }

                if (candidates.Count == 1)
                {
                    SelectUnit(candidates[0]);
                }
                else
                {
                    EnterAmbiguousState(candidates);
                }
            }
        }
        
        public void MovingBattalion(Vector2Int mapPosition)
        {
            if (!IsUnitSelected)
            {
                Debug.Log("No battalion selected.");
                return;
            }
            if (IsUnitMoving) return;
            switch (CurrentStage)
            {
                case Stage.Arrangement:
                    RemoveUnitFromMap(SelectedUnit);
                    SelectedUnit.position = mapPosition;
                    SelectedUnit.transform.localPosition = MapToLocal(SelectedUnit.position);
                    PlaceUnitOnMap(SelectedUnit, SelectedUnit.position);
                    break;
				
                case Stage.Battle:
                    if (CurrentActionStage != PlayerActionStage.MovingBattalion) break;
                    StartCoroutine(PerformPlayerMove(SelectedUnit, mapPosition));
                    break;
				
                default:
                    break;
            }
			
        }


        #endregion
    }
}