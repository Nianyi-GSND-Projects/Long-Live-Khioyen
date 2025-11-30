using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		[Header("Construction")]
		public Grid grid;
		public NavMeshSurface navMeshSurface;

		#region Ground
		void ConstructGround()
		{
			var ground = new GameObject("Ground").transform;
			ground.SetParent(transform, false);
			for(int x = 0; x < Size.x; ++x)
			{
				for(int y = 0; y < Size.y; ++y)
				{
					var tile = Instantiate(Resources.Load<GameObject>("Prefabs/Polis/Construction/Ground_tile"));
					tile.name = $"Polis Ground Tile ({x}, {y})";
					tile.transform.SetParent(ground.transform, false);
					tile.transform.localPosition = grid.CellToLocalInterpolated(new(x, 0, y));
				}
			}
		}

		struct WallConstructionParams
		{
			public int length;
			public Vector3 offset;
			public Vector3 space;
			public int orientation;
		}
		void ConstructWalls()
		{
			var sectionTemplate = Resources.Load<GameObject>("Prefabs/Polis/Construction/Wall_section");
			var cornerTemplate = Resources.Load<GameObject>("Prefabs/Polis/Construction/Wall_corner");
			var root = new GameObject("Walls").transform;
			root.SetParent(transform, false);
			var ps = new WallConstructionParams[] {
				new() {
					length = Size.x,
					offset = new Vector3(0, 0, -1),
					space = Vector3.right,
					orientation = 0,
				},
				new() {
					length = Size.y,
					offset = new Vector3(Size.x + 1, 0, 0),
					space = Vector3.forward,
					orientation = 3,
				},
				new() {
					length = Size.x,
					offset = new Vector3(Size.x, 0, Size.y + 1),
					space = Vector3.left,
					orientation = 2,
				},
				new() {
					length = Size.y,
					offset = new Vector3(-1, 0, Size.y),
					space = Vector3.back,
					orientation = 1,
				},
			};
			void MakeWall(GameObject template, Vector3 pos, int orientation)
			{
				var model = Instantiate(template);
				model.transform.SetParent(root, false);
				model.transform.SetLocalPositionAndRotation(
					pos,
					Quaternion.Euler(0, 90 * orientation, 0)
				);
				var obstacle = model.AddComponent<NavMeshObstacle>();
				obstacle.carving = true;
				obstacle.size = new Vector3(1, 4, 1);
				obstacle.center = Vector3.up * 2;
			}
			foreach(var p in ps)
			{
				for(int i = 0; i < p.length; ++i)
					MakeWall(sectionTemplate, i * p.space + p.offset, p.orientation);
				MakeWall(cornerTemplate, -1 * p.space + p.offset, p.orientation);
			}
		}

		public bool RayToGround(Ray ray, out Vector3 ground)
		{
			var plane = new Plane(Vector3.up, Vector3.zero);
			if(!plane.Raycast(ray, out float t))
			{
				ground = default;
				return false;
			}
			ground = ray.GetPoint(t);
			return true;
		}

		public bool ScreenToGround(Vector3 screen, out Vector3 ground)
		{
			var ray = Camera.main.ScreenPointToRay(screen);
			return RayToGround(ray, out ground);
		}

		Vector3 ClosestWalkablePosition(Vector3 reference)
		{
			int areaMask = 1 << NavMesh.GetAreaFromName("Walkable");
			NavMeshHit hit;
			if(NavMesh.SamplePosition(reference, out hit, 0.1f, areaMask))
				return hit.position;
			if(NavMesh.SamplePosition(reference, out hit, Size.magnitude, areaMask))
				return hit.position;
			Debug.LogWarning("Failed to find closest walkable position on the NavMesh.");
			return transform.position;
		}
		#endregion

		#region Grid
		public Vector2 WorldToMap(Vector3 world)
		{
			var cell = grid.LocalToCellInterpolated(grid.WorldToLocal(world));
			return new(cell.x, cell.z);
		}
		public Vector2Int WorldToMapInt(Vector3 world)
			=> Vector2Int.FloorToInt(WorldToMap(world));
		public Vector3 MapToWorld(Vector2 map)
			=> grid.LocalToWorld(MapToLocal(map));
		public Vector3 MapToLocal(Vector2 map)
			=> grid.CellToLocalInterpolated(new(map.x, 0, map.y));

		public Vector3 ClampToMap(Vector3 pos)
		{
			pos = WorldToMap(pos);
			pos.x = Mathf.Clamp(pos.x, 0, Size.x);
			pos.y = Mathf.Clamp(pos.y, 0, Size.y);
			return MapToWorld(pos);
		}

		public bool IsValidMapPosition(Vector2Int pos)
		{
			return pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y;
		}
		#endregion

		#region Occupancy
		IBuildingLike[,] occupancy;
		public System.Action onOccupancyChanged;

		IBuildingLike GetBuildingAt(int x, int y)
		{
			return occupancy[x, y];
		}

		IEnumerable<Vector2Int> YieldBuildingOccupancy(BuildingDefinition definition, BuildingPlacement placement)
		{
			// GPT gen

			// Local helper to rotate a grid vector by k quarter turns around +Y (same as Transform.Rotate(0, k*90, 0)).
			// Mapping follows Unity's left-handed transform convention:
			//   0: (x, y) -> ( x,  y)
			//   1: (x, y) -> ( y, -x)
			//   2: (x, y) -> (-x, -y)
			//   3: (x, y) -> (-y,  x)
			static Vector2Int Rot90(Vector2Int v, int quarterTurns)
			{
				quarterTurns = ((quarterTurns % 4) + 4) % 4; // normalize to {0,1,2,3}
				return quarterTurns switch
				{
					0 => v,
					1 => new Vector2Int(v.y, -v.x),
					2 => new Vector2Int(-v.x, -v.y),
					3 => new Vector2Int(-v.y, v.x),
					_ => v // unreachable
				};
			}

			var size = definition.size;          // rectangle size in cells at orientation=0
			var pivot = definition.pivot;         // rotation pivot in local (orientation=0) cell coords
			var origin = placement.position;       // world/grid coords where the pivot is placed
			int rot = placement.orientation & 3;

			// Enumerate every cell of the footprint (orientation=0 local space),
			// rotate the offset around the pivot, then translate to world/grid space.
			for(int ly = 0; ly < size.y; ly++)
			{
				for(int lx = 0; lx < size.x; lx++)
				{
					// Local cell (lx, ly) relative to pivot
					var deltaLocal = new Vector2Int(lx - pivot.x, ly - pivot.y);

					// Rotate around pivot and translate by 'origin'
					var deltaWorld = Rot90(deltaLocal, rot);
					yield return origin + deltaWorld;
				}
			}
		}

		public bool ValidateBuildingPlacement(BuildingDefinition definition, BuildingPlacement placement)
		{
			foreach(var pos in YieldBuildingOccupancy(definition, placement))
			{
				if(!IsValidMapPosition(pos))
					return false;
				if(occupancy[pos.x, pos.y] != null)
					return false;
			}
			return true;
		}
		#endregion

		#region Building
		void SpawnBuildingsFromData()
		{
			occupancy = new IBuildingLike[Size.x, Size.y];

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
			InitializeBuildingLike(building, placement);
		}

		void SpawnConstructionSite(BuildingPlacement placement)
		{
			var go = Instantiate(Resources.Load<GameObject>($"Models/Buildings/{placement.id}"));
			var site = go.AddComponent<ConstructionSite>();
			InitializeBuildingLike(site, placement);
		}

		void InitializeBuildingLike(IBuildingLike building, BuildingPlacement placement)
		{
			if(!GameManager.FindBuildingDefinitionByType(placement.id, out var definition))
			{
				Debug.LogWarning($"Skipping spawning building of ID \"{placement.id}\", cannot find its definition.");
				return;
			}

			PositionBuilding((building as Component).transform, definition, placement);
			building.Definition = definition;
			building.Placement = placement;

			foreach(var pos in YieldBuildingOccupancy(definition, placement))
				occupancy[pos.x, pos.y] = building;
			onOccupancyChanged?.Invoke();
		}

		void RemoveBuildingLike(IBuildingLike building)
		{
			var definition = building.Definition;
			var placement = building.Placement;

			Destroy((building as Component).gameObject);

			foreach(var pos in YieldBuildingOccupancy(definition, placement))
				occupancy[pos.x, pos.y] = null;
			onOccupancyChanged?.Invoke();
		}

		void FinishConstruction(ConstructionSite site)
		{
			BuildingPlacement placement = site.Placement;
			RemoveBuildingLike(site);
			SpawnBuilding(placement);
		}

		public void PositionBuilding(Transform building, BuildingDefinition definition, BuildingPlacement placement)
		{
			building.SetParent(transform, false);
			Vector2 planar = (Vector2)definition.size - definition.center - definition.pivot;
			building.localPosition = MapToLocal(placement.position) + new Vector3(planar.x, 0, planar.y);
			building.localEulerAngles = Vector3.up * (placement.orientation * 90);
		}

		public void ConstructBuilding(string type, Vector2Int mapPosition, int orientation)
		{
			if(!GameManager.FindBuildingDefinitionByType(type, out var definition))
				return;
			BuildingPlacement placement = new()
			{
				id = type,
				position = mapPosition,
				orientation = orientation,
				underConstruction = true,
			};
			SpawnConstructionSite(placement);
			Data.buildings.Add(placement);

			PolisTask task = new()
			{
				type = PolisTaskType.construction,
				parameters = new string[] {
					mapPosition.x.ToString(),
					mapPosition.y.ToString(),
				},
				remainingTime = definition.constructionTime,
			};
			AddTask(task);
		}
		#endregion
	}
}
