using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class ConstructModal : MonoBehaviour
	{
		public MayorMode mayorMode;
		int orientation;
		Polis Polis => Polis.Instance;
		public LayoutGroup constructOptionsUi;

		#region Life cycle
		void OnEnable()
		{
			GenerateUi();
			SelectedBuildingType = null;
			ShowCostPreview = false;
			Polis.onEconomyChanged += OnEconomyDataChanged;
			Polis.onOccupancyChanged += UpdatePreviewModel;
		}

		void OnDisable()
		{
			SelectedBuildingType = null;
		}

		void OnEconomyDataChanged()
		{
			if(SelectedBuildingType != null)
			{
				if(!(SelectedBuildingType.cost <= Polis.Economy))
					SelectedBuildingType = null;
			}
		}
		#endregion

		#region Input handlers
		protected void OnRotateBuilding()
		{
			orientation = (orientation + 1) % 4;
			UpdatePreviewModel();
		}

		protected void OnDrag()
		{
			UpdatePreviewModel();
		}
		#endregion

		#region UI
		void GenerateUi()
		{
			List<Transform> children = new();
			for(int i = 0; i < constructOptionsUi.transform.childCount; ++i)
				children.Add(constructOptionsUi.transform.GetChild(i));
			foreach(var child in children)
				Destroy(child.gameObject);

			var cardTemplate = Resources.Load<GameObject>("Prefabs/Polis/UI/Construct Option Card");
			foreach(var definition in GameManager.BuildingDefinitions)
			{
				var card = Instantiate(cardTemplate).GetComponent<ConstructOptionCard>();
				card.buildingDefinition = definition;
				card.transform.SetParent(constructOptionsUi.transform, false);

				card.onSelected += OnConstructionCardSelected;
				card.onHovered += OnConstructionCardHovered;
				card.onUnhovered += OnConstructionCardUnhovered;
			}
			constructOptionsUi.CalculateLayoutInputHorizontal();
		}

		void OnConstructionCardSelected(ConstructOptionCard card)
		{
			SelectedBuildingType = card.buildingDefinition;
		}

		void OnConstructionCardHovered(ConstructOptionCard card)
		{
			hoveredBuildingType = card.buildingDefinition;
			ShowCostPreview = true;
		}

		void OnConstructionCardUnhovered(ConstructOptionCard card)
		{
			ShowCostPreview = false;
		}
		#endregion

		#region Selection
		BuildingDefinition selectedBuildingType, hoveredBuildingType;
		ConstructPreview preview;
		public BuildingDefinition SelectedBuildingType
		{
			get => selectedBuildingType;
			set
			{
				if(value == selectedBuildingType)
					return;

				if(preview != null)
				{
					Destroy(preview.gameObject);
					preview = null;
				}

				selectedBuildingType = value;

				if(selectedBuildingType != null)
				{
					orientation = selectedBuildingType.defaultOrientation;
					preview = new GameObject("Construction Preview").AddComponent<ConstructPreview>();
					preview.Definition = selectedBuildingType;
					preview.transform.SetParent(Polis.transform, false);
					preview.onInitialized += UpdatePreviewModel;
				}
			}
		}

		void UpdatePreviewModel()
		{
			if(preview == null)
				return;

			Vector3 groundPos;
			Vector2Int mapPos = default;
			bool PositionMakesSense()
			{
				if(!Polis.ScreenToGround(mayorMode.PointerScreenPosition, out groundPos))
					return false;
				if(!Polis.IsValidMapPosition(mapPos = Polis.WorldToMapInt(groundPos)))
					return false;
				return true;
			}
			if(!PositionMakesSense())
			{
				preview.Visible = false;
				return;
			}
			preview.Visible = true;

			BuildingPlacement placement = new()
			{
				position = mapPos,
				orientation = orientation,
			};
			preview.Valid = Polis.ValidateBuildingPlacement(SelectedBuildingType, placement);
			Polis.PositionBuilding(preview.transform, SelectedBuildingType, placement);
		}
		#endregion

		#region Cost preview
		[SerializeField] CostPreviewPanel costPreviewPanel;
		bool ShowCostPreview
		{
			get => costPreviewPanel.gameObject.activeSelf;
			set
			{
				if(hoveredBuildingType == null)
					value = false;
				if(value == ShowCostPreview)
					return;

				costPreviewPanel.gameObject.SetActive(value);
				if(value)
					costPreviewPanel.UpdateCostData(hoveredBuildingType.cost);
			}
		}
		#endregion

		#region Actions
		public void TryPlaceBuilding()
		{
			if(SelectedBuildingType == null)
				return;
			if(!Polis.ScreenToGround(mayorMode.PointerScreenPosition, out Vector3 groundPosition))
				return;

			BuildingPlacement placement = new()
			{
				position = Polis.WorldToMapInt(groundPosition),
				orientation = orientation,
			};
			if(!Polis.ValidateBuildingPlacement(SelectedBuildingType, placement))
			{
				Debug.LogWarning($"Cannot place {SelectedBuildingType.id} at {placement.position}, obstructed.");
				return;
			}

			if(!Polis.TryCostResource(SelectedBuildingType.cost))
			{
				Debug.LogWarning(
					$"Not enough resources to build {SelectedBuildingType.id}!\n" +
					$"Required: {SelectedBuildingType.cost}, current: {Polis.Economy}."
				);
				return;
			}

			Polis.ConstructBuilding(SelectedBuildingType.id, placement.position, orientation);
		}
		#endregion
	}
}
