using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
    public class ArrangementModal : MonoBehaviour
    {
        public ArrangementMode arrangementMode;
        
        Battle Battle=> Battle.Instance;
        
        public LayoutGroup ArrangementLayoutGroup;
        
        #region Life cycle
		void OnEnable()
		{
			
			//ShowCostPreview = false;
			//Polis.onEconomyDataChanged += OnEconomyDataChanged;
			//Polis.onBuildingOccupancyChanged += UpdatePreviewModel;
		}
  //
  //       void OnDisable()
  //       {
  //           SelectedBuildingType = null;
  //       }
  //       
  //       void OnEconomyDataChanged()
  //       {
  //           if(SelectedBuildingType != null)
  //           {
  //               if(!(SelectedBuildingType.cost <= Polis.Economy))
  //                   SelectedBuildingType = null;
  //           }
  //       }
        #endregion
  //       
         #region Input handlers
  //
		// protected void OnDrag()
		// {
		// 	UpdatePreviewModel();
		// }
		 #endregion
  //
		 #region UI

		 public void InitializeUi()
		 {
			 GenerateUi();
			 Debug.Log("Arrangement Modal Enabled");
			 Battle.SelectedBattalionDescriptor = null;
		 }
		void GenerateUi()
		{
			
			List<Transform> children = new();
			for(int i = 0; i < ArrangementLayoutGroup.transform.childCount; ++i)
				children.Add(ArrangementLayoutGroup.transform.GetChild(i));
			foreach(var child in children)
				Destroy(child.gameObject);
  
			var cardTemplate = Resources.Load<GameObject>("Prefabs/Battle/UI/Battalion_Arrangement");
			foreach(var reserveTeam in Battle.playerReserveTeam)
			{
				var card = Instantiate(cardTemplate).GetComponent<BattalionArrangementUi>();
				card.Setup(reserveTeam); 
				card.transform.SetParent(ArrangementLayoutGroup.transform, false);
  
				card.onSelected += OnBattalionCardSelected;
				card.onHovered += OnBattalionCardHovered;
				card.onUnhovered += OnBattalionCardUnhovered;
			}
			ArrangementLayoutGroup.CalculateLayoutInputHorizontal();
		}
  //
		void OnBattalionCardSelected(BattalionArrangementUi card)
		{
			if (!card.battalionDescriptor.placed)
			{
				Battle.SelectedBattalionDescriptor = card.battalionDescriptor;
				Battle.IsReserveTeamSelected = true;
			}
		}
  
		void OnBattalionCardHovered(BattalionArrangementUi card)
		{
			_hoveredBattalionDescriptor = card.battalionDescriptor;
			//ShowCostPreview = true;
		}
  
		void OnBattalionCardUnhovered(BattalionArrangementUi card)
		{
			//ShowCostPreview = false;
		}
		 #endregion

		#region Selection
		BattalionDescriptor  _hoveredBattalionDescriptor;
		
		//ArrangementPreview preview;
		// void UpdatePreviewModel()
		// {
		// 	if(preview == null)
		// 		return;
		//
		// 	Vector3 groundPos;
		// 	Vector2Int mapPos = default;
		// 	bool PositionMakesSense()
		// 	{
		// 		if(!Polis.ScreenToGround(mayorMode.PointerScreenPosition, out groundPos))
		// 			return false;
		// 		if(!Polis.IsValidMapPosition(mapPos = Polis.WorldToMapInt(groundPos)))
		// 			return false;
		// 		return true;
		// 	}
		// 	if(!PositionMakesSense())
		// 	{
		// 		preview.Visible = false;
		// 		return;
		// 	}
		// 	preview.Visible = true;
		//
		// 	BuildingPlacement placement = new()
		// 	{
		// 		position = mapPos,
		// 		orientation = orientation,
		// 	};
		// 	preview.Valid = Polis.ValidateBuildingPlacement(SelectedBuildingType, placement);
		// 	Polis.PositionBuilding(preview.transform, SelectedBuildingType, placement);
		// }
		#endregion
		
		#region Actions

		public void TryPlaceReserveTeam()
		{
			if (!Battle.ScreenToGround(arrangementMode.PointerScreenPosition, out Vector3 groundPosition))
			{
				Debug.LogWarning("Position not valid." + Battle.WorldToMapInt(groundPosition));
				return;
			}
			
			if (Battle.SelectedBattalionDescriptor == null)
			{
				Debug.LogWarning("No reserve team selected." + Battle.WorldToMapInt(groundPosition));
				return;
			}
			
			if (Battle.SelectedBattalionDescriptor.placed == true)
			{
				Debug.LogWarning("Reserve team already placed." + Battle.WorldToMapInt(groundPosition));
				return;
			}

			
			Vector2Int position = Battle.WorldToMapInt(groundPosition);
			if (!Battle.ValidateArrangementPlacement(position))
			{
				Debug.LogWarning($"Cannot place reserve team at {position}.");
				return;
			}
			Battle.PlacingPlayerBattalion(Battle.SelectedBattalionDescriptor, position);
		}
		
		public void TryMoveBattalionArrangement()
		{
			
			if (!Battle.ScreenToGround(arrangementMode.PointerScreenPosition, out Vector3 groundPosition))
			{
				Debug.LogWarning("Position not valid." + Battle.WorldToMapInt(groundPosition));
				return;
			}
			
			if (Battle.SelectedUnit == null)
			{
				Debug.LogWarning("No battalion selected." + Battle.WorldToMapInt(groundPosition));
				return;
			}

			if (Battle.SelectedUnit is not Battalion bat)
			{
				Debug.LogWarning("No a battalion!");
				return;
			}
			
			if (!Battle.ValidateArrangementPlacement(Battle.WorldToMapInt(groundPosition)))
			{
				Debug.LogWarning($"Cannot move {Battle.SelectedUnit.InstanceId} at {Battle.WorldToMapInt(groundPosition)}.");
				return;
			}
			Battle.MovingBattalion(Battle.WorldToMapInt(groundPosition));
		}
		
		public void TryMoveBattalionBattle()
		{
			if (Battle.CurrentTurnState != TurnState.PlayerTurn)
			{
				Debug.LogWarning("Not player turn!");
				return;
			}
			if (!Battle.ScreenToGround(arrangementMode.PointerScreenPosition, out Vector3 groundPosition))
			{
				Debug.LogWarning("Position not valid." + Battle.WorldToMapInt(groundPosition));
				return;
			}
			
			if (Battle.SelectedUnit == null)
			{
				Debug.LogWarning("No battalion selected." + Battle.WorldToMapInt(groundPosition));
				return;
			}

			if (Battle.SelectedUnit.actionDone == true)
			{
				Debug.LogWarning("Battalion already acted this turn!");
				return;
			}
			if (!Battle.ValidateArrangementPlacement(Battle.WorldToMapInt(groundPosition)))
			{
				Debug.LogWarning($"Cannot move {Battle.SelectedUnit.InstanceId} at {Battle.WorldToMapInt(groundPosition)}.");
				return;
			}

			if (!Battle.TestAvailableMovePositions(Battle.WorldToMapInt(groundPosition)))
			{
				Debug.LogWarning($"Out of range!");
				return;
			}
			Battle.MovingBattalion(Battle.WorldToMapInt(groundPosition));
		}
		
		public void TryApplyCurrentAction()
		{
			if (Battle.CurrentTurnState != TurnState.PlayerTurn)
			{
				Debug.LogWarning("Not player turn!");
				return;
			}
			if (!Battle.ScreenToGround(arrangementMode.PointerScreenPosition, out Vector3 groundPosition))
			{
				Debug.LogWarning("Position not valid." + Battle.WorldToMapInt(groundPosition));
				return;
			}
			
			if (Battle.SelectedUnit == null)
			{
				Debug.LogWarning("No battalion selected." + Battle.WorldToMapInt(groundPosition));
				return;
			}

			if (Battle.SelectedUnit.actionDone == true)
			{
				Debug.LogWarning("Battalion already acted this turn!");
				return;
			}
			
			if (!Battle.Instance.IsTargetPositionValid(Battle.WorldToMapInt(groundPosition)))
			{
				Debug.LogWarning($"Not Valid Target!");
				return;
			}
			
			Battle.ApplyAction(Battle.WorldToMapInt(groundPosition));
		}
		#endregion
    }
}
