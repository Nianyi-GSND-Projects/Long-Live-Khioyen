using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		#region 生命周期
		void InitializeBuilding()
		{
			Data.onConstructionSitePlaced += OnConstructionSitePlace;
			Data.onBuildingConstructed += OnBuildingConstructed;
			Data.onBuildingChanged += OnBuildingChanged;
			Data.onBuildingRemoved += OnBuildingRemoved;
		}

		void FinalizeBuilding()
		{
			Data.onConstructionSitePlaced -= OnConstructionSitePlace;
			Data.onBuildingConstructed -= OnBuildingConstructed;
			Data.onBuildingChanged -= OnBuildingChanged;
			Data.onBuildingRemoved -= OnBuildingRemoved;
		}
		#endregion

		#region 事件
		void OnConstructionSitePlace(BuildingPlacement placement)
		{
			SpawnConstructionSite(placement);
		}

		void OnBuildingConstructed(BuildingPlacement placement)
		{
			FinishConstruction(placement);
		}

		void OnBuildingChanged(BuildingPlacement placement)
		{
			PositionBuilding((buildings[placement] as Component).transform, placement);
		}

		void OnBuildingRemoved(BuildingPlacement placement)
		{
			DestroyBuildingLike(placement);
		}
		#endregion

		#region 辅助
		Dictionary<BuildingPlacement, IBuildingLike> buildings = new();

		void SpawnBuildingsFromData()
		{
			foreach(var placement in Data.buildings)
			{
				if(placement.underConstruction)
					SpawnConstructionSite(placement);
				else
					SpawnBuilding(placement);
			}
		}

		void SpawnBuilding(BuildingPlacement placement)
		{
			var go = Instantiate(Resources.Load<GameObject>($"Prefabs/Polis/Buildings/{placement.id}"));
			var building = go.GetComponent<Building>();
			RecordAndPlaceBuildingLike(placement, building);
		}

		void SpawnConstructionSite(BuildingPlacement placement)
		{
			var go = Instantiate(Resources.Load<GameObject>($"Models/Buildings/{placement.id}"));
			var site = go.AddComponent<ConstructionSite>();
			RecordAndPlaceBuildingLike(placement, site);
		}

		void RecordAndPlaceBuildingLike(BuildingPlacement placement, IBuildingLike building)
		{
			if(!GameManager.FindBuildingDefinitionById(placement.id, out var definition))
			{
				Debug.LogWarning($"Skipping spawning building of ID \"{placement.id}\", cannot find its definition.");
				return;
			}

			PositionBuilding((building as Component).transform, placement);
			building.Definition = definition;
			building.Placement = placement;

			buildings[placement] = building;
		}

		void DestroyBuildingLike(BuildingPlacement placement)
		{
			Destroy((buildings[placement] as Component).gameObject);
			buildings.Remove(placement);
		}

		void FinishConstruction(BuildingPlacement placement)
		{
			DestroyBuildingLike(placement);
			SpawnBuilding(placement);
		}

		public void PositionBuilding(Transform building, BuildingPlacement placement)
		{
			var definition = placement.Definition;
			building.SetParent(transform, false);
			Vector2 planar = (Vector2)definition.size - definition.center - definition.pivot;
			building.localPosition = MapToLocal(placement.position) + new Vector3(planar.x, 0, planar.y);
			building.localEulerAngles = Vector3.up * (placement.orientation * 90);
		}
		#endregion
	}
}
