using Cinemachine;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Rendering.DebugUI;

namespace LongLiveKhioyen
{
	public class Polis : MonoBehaviour
	{
		static Polis instance;
		public static Polis Instance => instance;

		#region Life cycle
		public System.Action onInitialized;

		void Awake()
		{
			instance = this;
		}

		void Start()
		{
			GameInstance.Instance.ExecuteWhenInitialized(Initialize);
		}

		void Initialize()
		{
			if(Data == null)
				player.gameObject.SetActive(false);

			SwitchToMode(Mode.Mayor);
			IsInConstructModal = false;

			/* Procedural polis generation */

			// Orientation
			transform.rotation = Quaternion.Euler(0, Data.orientation, 0);
			gameObject.isStatic = true;

			// Ground
			ConstructGround();

			// Walls
			ConstructWalls();

			// Buildings
			buildingOccupancy = new Building[Size.x, Size.y];
			foreach(var placement in Data.buildings)
				SpawnBuilding(placement);

			// Initialize Navmesh
			navMeshSurface.RemoveData();
			navMeshSurface.BuildNavMesh();

			// Center view
			AnchorPosition = MapToWorld((Vector2)Size * .5f);

			/* Time */

			GameInstance.Instance.onGameTimeAdvanced += PassTime;

			// Update accumulated status changes since last leaving
			PassTime(GameInstance.Instance.GameTime - LastTime);

			onInitialized?.Invoke();
		}

		void OnDestroy()
		{
			instance = null;
			if(GameInstance.Instance)
				GameInstance.Instance.onGameTimeAdvanced -= PassTime;
		}

		void Update()
		{
			float dt = Time.deltaTime;
			if(dt > 0)
				GameInstance.Instance.AdvanceTime(dt);
		}
		#endregion

		#region Data
		PolisData Data => GameInstance.Instance.LastPolis;

		public string Id => Data.id;
		public Vector2Int Size => Data.size;
		#endregion

		#region Population
		public System.Action onPopulationChanged;

		public int Population { get; private set; } = 10;  // Debug
		public int BusyPopulation { get; private set; } = 5;
		public int PopulationCap { get; private set; } = 12;
		#endregion

		#region Economy
		public Economy Economy
		{
			get => Data.economy;
			set => Data.economy = value;
		}

		public System.Action onEconomyChanged;

		public bool CheckResourceAffordance(Economy cost)
		{
			return cost <= Economy;
		}

		public bool TryCostResource(Economy cost, bool actuallyCost = true)
		{
			if(!CheckResourceAffordance(cost))
				return false;
			if(actuallyCost)
			{
				Economy -= cost;
				onEconomyChanged?.Invoke();
			}
			return true;
		}
		#endregion

		#region Construction
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
					var tile = Instantiate(Resources.Load<GameObject>("Prefabs/Polis/Ground_tile"));
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
			var sectionTemplate = Resources.Load<GameObject>("Prefabs/Polis/Wall_section");
			var cornerTemplate = Resources.Load<GameObject>("Prefabs/Polis/Wall_corner");
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

		#region Building
		readonly List<Building> buildings = new();
		Building[,] buildingOccupancy;
		public System.Action onBuildingOccupancyChanged;

		Building currentSelection;
		public Building SelectedBuilding
		{
			get => currentSelection;
			set
			{
				// 解除上一个的高亮
				if(currentSelection != null)
					currentSelection.Selected = false;

				currentSelection = value;

				// 新的高亮 & UI
				if(currentSelection != null)
				{
					currentSelection.Selected = true;
					// TODO: 打开信息面板 / 属性编辑器
					Debug.Log($"Seleted {currentSelection}.", currentSelection);
				}
				else
				{
					// TODO: 关闭信息面板
				}
			}
		}

		Building SpawnBuilding(BuildingPlacement placement)
		{
			if(!GameManager.FindBuildingDefinitionByType(placement.id, out var definition))
			{
				Debug.LogWarning($"Skipping spawning building of ID \"{placement.id}\", cannot find its definition.");
				return null;
			}

			var building = new GameObject().AddComponent<Building>();
			PositionBuilding(building.transform, definition, placement);
			buildings.Add(building);
			building.Placement = placement;
			building.Definition = definition;

			foreach(var pos in YieldBuildingOccupancy(definition, placement))
				buildingOccupancy[pos.x, pos.y] = building;

			return building;
		}

		Building GetBuildingAt(int x, int y)
		{
			return buildingOccupancy[x, y];
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
				if(buildingOccupancy[pos.x, pos.y] != null)
					return false;
			}
			return true;
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
			SpawnBuilding(placement);
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
		#endregion

		#region Control mode
		public enum Mode { Mayor, Wander }
		Mode currentMode = Mode.Mayor;
		public Mode CurrentMode => currentMode;

		public void SwitchMode()
		{
			switch(currentMode)
			{
				case Mode.Mayor:
					SwitchToMode(Mode.Wander);
					break;
				case Mode.Wander:
					SwitchToMode(Mode.Mayor);
					break;
			}
		}

		public void SwitchToMode(Mode mode)
		{
			if(mode != Mode.Mayor)
				SetMayorMode(false);
			if(mode != Mode.Wander)
				SetWanderMode(false);

			if(mode == Mode.Mayor)
				SetMayorMode(true);
			if(mode == Mode.Wander)
				SetWanderMode(true);

			currentMode = mode;
		}

		[Header("Control Mode")]
		public Transform anchor;
		public Vector3 AnchorPosition
		{
			get => anchor.position;
			set => anchor.position = ClampToMap(value);
		}
		public Vector3 AnchorEulers
		{
			get => anchor.eulerAngles;
			set => anchor.eulerAngles = value;
		}
		public float MayorDistance
		{
			get => -mayorCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset.z;
			set
			{
				var composer = mayorCamera.GetCinemachineComponent<CinemachineTransposer>();
				var offset = composer.m_FollowOffset;
				offset.z = -value;
				composer.m_FollowOffset = offset;
			}
		}

		public ConstructModal constructModal;
		public bool IsInConstructModal
		{
			get => constructModal.gameObject.activeInHierarchy;
			set
			{
				if(CurrentMode != Mode.Mayor)
					value = false;
				constructModal.gameObject.SetActive(value);
			}
		}

		#region Mayor mode
		[SerializeField] CinemachineVirtualCamera mayorCamera;

		void SetMayorMode(bool enabled)
		{
			mayorCamera.enabled = enabled;
			if(enabled)
				AnchorPosition = Vector3.ProjectOnPlane(player.transform.position, Vector3.up);
		}
		#endregion

		#region Wander mode
		[SerializeField] AbstractCharacterController player;
		[SerializeField] CinemachineVirtualCamera wanderCamera;

		void SetWanderMode(bool enabled)
		{
			wanderCamera.enabled = enabled;
			player.gameObject.SetActive(enabled);
			player.GetComponent<NavMeshAgent>().enabled = enabled;
			if(enabled)
			{
				AnchorPosition = ClosestWalkablePosition(AnchorPosition);
				player.Teleport(AnchorPosition);
				player.FaceTowards(Camera.main.transform.forward);
			}
		}
		#endregion
		#endregion

		#region Time
		float LastTime
		{
			get => Data.lastTime;
			set => Data.lastTime = value;
		}

		void PassTime(float amount)
		{
			while(amount > 0)
			{
				if(Tasks.Count == 0)
				{
					PassTime_Simple(amount);
					return;
				}
				float a = Mathf.Min(amount, Tasks[0].remainingTime);
				PassTime_Simple(a);
				amount -= a;
			}
		}

		void PassTime_Simple(float amount)
		{
			foreach(var task in Tasks)
				task.remainingTime -= amount;
			var toBeExecuted = Tasks.Where(t => t.remainingTime <= 0).ToArray();
			foreach(var task in toBeExecuted)
			{
				ExecuteTask(task);
				Tasks.Remove(task);
			}
			LastTime += amount;
		}
		#endregion

		#region Tasks
		IList<PolisTask> Tasks => Data.Tasks;

		public void AddTask(PolisTask task)
		{
			Data.AddTask(task);
		}

		void ExecuteTask(PolisTask task)
		{
			switch(task.type)
			{
				case PolisTaskType.construction:
					ExecuteConstructionTask(task);
					break;
				case PolisTaskType.monthPassed:
					ExecuteMonthPassedTask(task);
					break;
				default: throw new System.NotSupportedException();
			}
		}

		void ExecuteConstructionTask(PolisTask task)
		{
			int x = int.Parse(task.parameters[0]), y = int.Parse(task.parameters[1]);
			var building = GetBuildingAt(x, y);
			if(building == null)
			{
				Debug.LogError($"No building at ({x}, {y}).");
				return;
			}
			building.UnderConstruction = false;
		}

		void ExecuteMonthPassedTask(PolisTask task)
		{
			int startingMonth = int.Parse(task.parameters[0]);
			Debug.Log($"Month passed in polis \"{Data.id}\". Starting month: {startingMonth}");
			// TODO
		}
		#endregion
	}
}
