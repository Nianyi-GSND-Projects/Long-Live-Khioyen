using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class ConstructModal : MonoBehaviour
	{
		public MayorMode mayorMode;
		int orientation;
		public LayoutGroup constructOptionsUi;

		#region Life cycle
		void OnEnable()
		{
			GenerateUi();
			SelectedBuildingType = null;
			ShowCostPreview = false;
			PolisData.Current.onEconomyChanged += OnEconomyDataChanged;
			PolisData.Current.onBuildingsChanged += UpdatePreviewModel;
		}

		void OnDisable()
		{
			PolisData.Current.onEconomyChanged -= OnEconomyDataChanged;
			SelectedBuildingType = null;
		}

		void OnEconomyDataChanged()
		{
			if(SelectedBuildingType != null)
			{
				if(!PolisData.Current.Economy.CanCover(SelectedBuildingType.cost))
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
			foreach(var definition in GameManager.BuildingDefinitions.Where(PolisData.Current.CanConstructBuilding))
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
					preview.transform.SetParent(Polis.Instance.transform, false);
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
				if(!Polis.Instance.ScreenToGround(mayorMode.PointerScreenPosition, out groundPos))
					return false;
				if(!PolisData.Current.IsValidMapPosition(mapPos = Polis.Instance.WorldToMapInt(groundPos)))
					return false;
				return true;
			}
			if(!PositionMakesSense())
			{
				preview.Visible = false;
				return;
			}
			preview.Visible = true;

			BuildingPlacement placement = new(SelectedBuildingType.id, mapPos, orientation);
			preview.Valid = PolisData.Current.ValidateBuildingPlacement(placement);
			Polis.Instance.PositionBuilding(preview.transform, placement);
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
			if(!Polis.Instance.ScreenToGround(mayorMode.PointerScreenPosition, out Vector3 groundPosition))
				return;

			BuildingPlacement placement = new(SelectedBuildingType.id, Polis.Instance.WorldToMapInt(groundPosition), orientation);
			if(!PolisData.Current.ValidateBuildingPlacement(placement))
			{
				Debug.LogWarning($"Cannot place {SelectedBuildingType.id} at {placement.position}, obstructed.");
				return;
			}

			// 先 try 再 cost 是因为如果直接 cost，SelectedBuildingType 会在 construct 之前变成 null，进而报错。
			if(!PolisData.Current.Economy.TryCost(SelectedBuildingType.cost, false))
			{
				Debug.LogWarning(
					$"Not enough resources to build {SelectedBuildingType.id}!\n" +
					$"Required: {SelectedBuildingType.cost}, current: {PolisData.Current.Economy}."
				);
				return;
			}
			PolisData.Current.ConstructBuilding(SelectedBuildingType.id, placement.position, orientation);
			PolisData.Current.Economy.Cost(SelectedBuildingType.cost);
		}
		#endregion
	}
}
