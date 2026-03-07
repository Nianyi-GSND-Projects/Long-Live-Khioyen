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

                if (value != null) 
                {
                    ClearUnitSelection();
                }
				
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
            
            if (!factionActiveUnits[Faction.Player].Contains(unit))
            {
                Debug.Log("Battalion " + unit.InstanceId + " is not your battalion.");
                if(CurrentStage == Stage.Battle) ClearAllHexRingHighlights();
                return;
            }
			
            switch (CurrentStage)
            {
                case Stage.Arrangement:
                    ClearReserveTeamSelection();
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
                    
                    if (CurrentActionStage == PlayerActionStage.None)
                    {
                        if (unit is Battalion battalion)
                        {
                            int moveRange = initialUnitMovement;
                            UpdateAvailableMovePositions(battalion);
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

        private void UpdateAvailableMovePositions(Battalion bat)
        {
            if (bat == null) return;
            ClearAllHexRingHighlights();
            var reachableTiles = GetAccessableTilesInRange(bat, bat.currentMovement, true);
            var visibleTiles = GetAllVisibleTiles();
            reachableTiles.IntersectWith(visibleTiles);
            availableMovePositions = reachableTiles;
            HighlightTilesRing(availableMovePositions, moveRingColor);
        }

        public HashSet<Vector2Int> GetAllVisibleTiles()
        {
            HashSet<Vector2Int> visibleTiles = new HashSet<Vector2Int>(); if (fogMap == null) return visibleTiles;
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    if (fogMap[x, y] == FogState.Visible)
                    {
                        visibleTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            return visibleTiles;
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
            if(CurrentStage == Stage.Battle) ClearAllHexRingHighlights();
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
