using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		[Header("Construction")]
		public Grid grid;
		public NavMeshSurface navMeshSurface;

		#region 生命周期
		void InitializeConstruction()
		{
			transform.rotation = Quaternion.Euler(0, Data.orientation, 0);
			gameObject.isStatic = true;
			// 建筑数据变化后，重算地面磨损贴图。
			Data.onBuildingsChanged += RecalculateWearnessMap;

			ConstructGround();
			ConstructWalls();
			SpawnBuildingsFromData();

			// 锚点归中
			AnchorPosition = MapToWorld((Vector2)Data.size * .5f);

			// Navmesh
			navMeshSurface.RemoveData();
			navMeshSurface.BuildNavMesh();

			// DEBUG: Plop NPCs
			var npcTemplate = Resources.Load<GameObject>("Prefabs/Polis/Characters/NPC-dummy");
			for(int i = 0; i < 100; ++i)
			{
				var position = Utilities.GetRandomPositionOnNavMesh(navMeshSurface);
				var npc = Instantiate(npcTemplate);
				npc.transform.SetParent(transform, false);
				npc.transform.position = position;
			}
		}

		void FinalizeConstruction()
		{
			Data.onBuildingsChanged -= RecalculateWearnessMap;
			DestructGround();
		}
		#endregion

		#region 地面
		Material groundMat;
		Texture2D wearnessMap;

		[Header("Wearness")]
		[Min(1)] public int wearnessSampleCount = 10;
		[Range(0f, 1f)] public float wearnessDecayPerRound = 0.6f;
		[Range(0f, 20f)] public float wearnessPenaltyScale = 5f;

		void ConstructGround()
		{
			GameObject template = Resources.Load<GameObject>("Prefabs/Polis/Construction/Ground_tile");

			groundMat = new(Resources.Load<Material>("Materials/Polis/Polis_ground-base"));
			// Initialize material
			groundMat.SetVector("_Size", new(Data.size.x, Data.size.y, 0, 0));
			groundMat.SetFloat("_Orientation", Data.orientation);
			wearnessMap = new(Data.size.x, Data.size.y, TextureFormat.RGBAHalf, false, true);
			wearnessMap.filterMode = FilterMode.Bilinear;
			wearnessMap.wrapMode = TextureWrapMode.Clamp;
			RecalculateWearnessMap();
			groundMat.SetTexture("_Wearness_Map", wearnessMap);

			var ground = new GameObject("Ground").transform;
			ground.SetParent(transform, false);
			for(int x = 0; x < Data.size.x; ++x)
			{
				for(int y = 0; y < Data.size.y; ++y)
				{
					var tile = Instantiate(template);
					tile.name = $"Polis Ground Tile ({x}, {y})";
					tile.GetComponentInChildren<Renderer>().sharedMaterial = groundMat;

					tile.transform.SetParent(ground.transform, false);
					tile.transform.localPosition = grid.CellToLocalInterpolated(new(x, 0, y));
				}
			}
		}

		void DestructGround()
		{
			Destroy(groundMat);
			Destroy(wearnessMap);
		}

		[ContextMenu("Recalculate Wearness Map")]
		void RecalculateWearnessMap()
		{
			Utilities.wearnessSampleCount = wearnessSampleCount;
			Utilities.wearnessDecayPerRound = wearnessDecayPerRound;
			Utilities.wearnessPenaltyScale = wearnessPenaltyScale;

			var map = Utilities.CalculateWearnessMap(Data);
			for(int x = 0; x < Data.size.x; ++x)
			{
				for(int y = 0; y < Data.size.y; ++y)
					wearnessMap.SetPixel(x, y, map[x, y]);
			}
			wearnessMap.Apply();
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
			if(NavMesh.SamplePosition(reference, out hit, Data.size.magnitude, areaMask))
				return hit.position;
			Debug.LogWarning("Failed to find closest walkable position on the NavMesh.");
			return transform.position;
		}
		#endregion

		#region 城墙
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
					length = Data.size.x,
					offset = new Vector3(0, 0, -1),
					space = Vector3.right,
					orientation = 0,
				},
				new() {
					length = Data.size.y,
					offset = new Vector3(Data.size.x + 1, 0, 0),
					space = Vector3.forward,
					orientation = 3,
				},
				new() {
					length = Data.size.x,
					offset = new Vector3(Data.size.x, 0, Data.size.y + 1),
					space = Vector3.left,
					orientation = 2,
				},
				new() {
					length = Data.size.y,
					offset = new Vector3(-1, 0, Data.size.y),
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
		#endregion
	}
}
