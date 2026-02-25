using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {

        #region BattalionDescriptor
        
        public BattalionDescriptor CurrentBattalionDescriptor{ get; set; }

        public BattalionDescriptor SelectedBattalionDescriptor
        {
            get => CurrentBattalionDescriptor;
            set
            {
                if (value == CurrentBattalionDescriptor)
                    return;
				
                if (value != null) ClearUnitSelection();
				
                CurrentBattalionDescriptor = value;
                IsReserveTeamSelected = (value != null);
                OnReserveTeamSelectionChanged?.Invoke(CurrentBattalionDescriptor);
				
            }
        }

        #endregion

        #region Unit
        
        public Unit CurrentUnit{ get; set; }
        public Unit SelectedUnit
        {
            get => CurrentUnit;
            set
            {
                if (value == CurrentUnit) return;
				
                if (CurrentUnit != null)
                    CurrentUnit.Selected = false;
				
                CurrentUnit = value;

                if (CurrentUnit != null)
                {
                    CurrentUnit.Selected = true;
                }
				
                OnUnitSelectionChanged?.Invoke(CurrentUnit);
            }
        }
        
        public void SelectUnit(Unit unit)
        {
            if (CurrentStage == Stage.Battle && CurrentTurnState != TurnState.PlayerTurn)
            {
                Debug.Log("Not your turn!");
                return;
            }

            SelectedUnit = unit;
            IsUnitSelected = true;
			
            if (IsReserveTeamSelected) 
                ClearReserveTeamSelection();
			
			
            if (!factionActiveUnits[Faction.Player].Contains(unit))
            {
                Debug.Log("Battalion " + unit.InstanceId + " is not your battalion.");
                if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
                return;
            }
			
            switch (CurrentStage)
            {
                case Stage.Arrangement:
                    break;
				
                case Stage.Battle:
					
                    if (CurrentTurnState != TurnState.PlayerTurn)
                    {
                        Debug.Log("Not Your Turn!");
                        return;
                    }
					
                    if (unit.actionDone)
                    {
                        Debug.Log("Battalion " + unit.InstanceId + " has already finished its action!");
                        break;
                    }
					
                    if (unit is Battalion bat && bat.currentMovement == 0)
                    {
                        Debug.Log("Battalion " + bat.InstanceId + " has no movement!");
                        break;
                    }
					
                    initialUnitPosition = SelectedUnit.position;
                    if(unit is Battalion battalion)
                        initialUnitMovement = battalion.currentMovement;
                    //TODO 可移动的设施？
                    if (CurrentActionStage == PlayerActionStage.None)
                    {
                        if (unit.unitDefinition.movable)
                        {
                            int moveRange = initialUnitMovement;
                            availableMovePositions = GetAccessableTilesInRange(SelectedUnit, moveRange);
                            ChangeActionStage(PlayerActionStage.MovingBattalion);
                        }
                        else if (unit.unitDefinition.actionable)
                        {
                            availableMovePositions.Clear();
                            ChangeActionStage(PlayerActionStage.SelectingAction);
                        }
                    }
					
                    break;
				
                default:
                    break;
            }
			
        }
        
        #endregion

        #region AmbiguousSelection
        private List<Unit> currentAmbiguousCandidates;
        
        private void EnterAmbiguousState(List<Unit> candidates)
        {
            currentAmbiguousCandidates = candidates;
            ChangeActionStage(PlayerActionStage.SelectingAmbiguousTarget);
        }
        public void ResolveAmbiguousSelection(Unit selectedUnit)
        {

            ChangeActionStage(PlayerActionStage.None);
			
            SelectUnit(selectedUnit);
        }
        #endregion

        #region Clear

        public void ClearAllSelection()
        {
            ClearReserveTeamSelection();
            ClearUnitSelection();
        }
        public void ClearReserveTeamSelection()
        {
            SelectedBattalionDescriptor = null;
            IsReserveTeamSelected = false;
        }
		
        public void ClearUnitSelection()
        {
            SelectedUnit = null;
            IsUnitSelected = false;
            if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
            availableMovePositions.Clear();
        }
        
        public void ClearAmbiguousSelection()
        {
            currentAmbiguousCandidates = null;
            OnAmbiguousSelectionEnded?.Invoke();
        }
        #endregion
    }
}
