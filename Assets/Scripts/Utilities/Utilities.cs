using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nianyi.UnityPack;
using System.Collections.Generic;
using System.Linq;
using WearnessTensor = UnityEngine.Vector3;

namespace LongLiveKhioyen
{
	public static class Utilities
	{
		public static T DeepCopy<T>(T source)
		{
			return JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
		}

		public static Vector3 GetRandomPositionOnNavMesh(this NavMeshSurface surface)
		{
			var bounds = surface.navMeshData.sourceBounds;
			for(int i = 0; i < 30; i++)
			{
				Vector3 random = new(
					Random.Range(bounds.min.x, bounds.max.x),
					Random.Range(bounds.min.y, bounds.max.y),
					Random.Range(bounds.min.z, bounds.max.z)
				);
				Vector3 world = surface.transform.TransformPoint(random);

				if(NavMesh.SamplePosition(world, out NavMeshHit hit, bounds.size.y, NavMesh.AllAreas))
					return hit.position;
			}
			return default;
		}

		public static void ClearChildren(this Transform parent)
		{
			if(parent == null)
				return;
			foreach(var c in parent.GetDirectChildren())
				c.gameObject.Destroy();
		}

		#region 城池地表磨损贴图计算
		/* 配置 */
		public static int wearnessSampleCount = 10;
		public static float wearnessDecayPerRound = 0.6f;
		public static float wearnessPenaltyScale = 5f;

		public static Color[,] CalculateWearnessMap(PolisData data)
		{
			/* 定义必要的变量 */

			int width = data.size.x, height = data.size.y;
			int[,] distances = new int[width, height];  // 建筑距离的 half SDF；在建筑内部是 0，在建筑外部是到建筑的曼哈顿距离。
			InitializeBuildingDistanceField(width, height, distances, data);
			List<Vector2Int> anchors = new(GetBuildingAnchors(data));  // 建筑寻路锚点列表。
			WearnessTensor[,] tensors = new WearnessTensor[width, height];  // 磨损张量，定义为 (cos(2*theta), sin(2*theta))，可加和。

			/* 算法步骤 */

			#region 寻路算法
			// AI gen

			// 复用寻路缓存，避免每次 FindPath 都分配 width*height 级别数组。
			var dirs8 = new Vector2Int[] {
				new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
				new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
			};
			var closedStamp = new int[width, height];
			var cameFrom = new Vector2Int[width, height];
			var gScore = new float[width, height];
			var gScoreStamp = new int[width, height];
			var openHeap = new List<(Vector2Int pos, float f)>();
			int pathStamp = 0;

			IEnumerable<Vector2Int> FindPath(Vector2Int pa, Vector2Int pb)
			{
				if(pa == pb)
					return new Vector2Int[] { pa };
				if(!data.IsValidMapPosition(pa) || !data.IsValidMapPosition(pb))
					return System.Array.Empty<Vector2Int>();
				if(pathStamp == int.MaxValue)
				{
					// 极端情况下重置 stamp，避免溢出。
					for(int x = 0; x < width; ++x)
					{
						for(int y = 0; y < height; ++y)
						{
							closedStamp[x, y] = 0;
							gScoreStamp[x, y] = 0;
						}
					}
					pathStamp = 0;
				}
				++pathStamp;

				void HeapPush((Vector2Int pos, float f) node)
				{
					openHeap.Add(node);
					int i = openHeap.Count - 1;
					while(i > 0)
					{
						int parent = (i - 1) / 2;
						if(openHeap[parent].f <= openHeap[i].f)
							break;
						(openHeap[parent], openHeap[i]) = (openHeap[i], openHeap[parent]);
						i = parent;
					}
				}

				(Vector2Int pos, float f) HeapPopMin()
				{
					var min = openHeap[0];
					int last = openHeap.Count - 1;
					openHeap[0] = openHeap[last];
					openHeap.RemoveAt(last);

					int i = 0;
					while(true)
					{
						int left = i * 2 + 1;
						if(left >= openHeap.Count)
							break;

						int right = left + 1;
						int smallest = (right < openHeap.Count && openHeap[right].f < openHeap[left].f) ? right : left;
						if(openHeap[i].f <= openHeap[smallest].f)
							break;

						(openHeap[i], openHeap[smallest]) = (openHeap[smallest], openHeap[i]);
						i = smallest;
					}

					return min;
				}

				static float Heuristic(Vector2Int a, Vector2Int b)
				{
					int dx = Mathf.Abs(a.x - b.x);
					int dy = Mathf.Abs(a.y - b.y);

					int min = Mathf.Min(dx, dy);
					int max = Mathf.Max(dx, dy);

					return (max - min) + Mathf.Sqrt(2) * min;
				}

				openHeap.Clear();
				cameFrom[pa.x, pa.y] = new Vector2Int(-1, -1);
				gScore[pa.x, pa.y] = 0f;
				gScoreStamp[pa.x, pa.y] = pathStamp;
				HeapPush((pa, Heuristic(pa, pb)));

				while(openHeap.Count > 0)
				{
					var currentNode = HeapPopMin();
					var current = currentNode.pos;

					// 惰性删除：堆里可能有旧条目，按最新 g+h 判定是否过期。
					if(gScoreStamp[current.x, current.y] != pathStamp)
						continue;
					float expectedF = gScore[current.x, current.y] + Heuristic(current, pb);
					if(currentNode.f > expectedF + 1e-5f)
						continue;
					if(closedStamp[current.x, current.y] == pathStamp)
						continue;

					if(current == pb)
						break;

					closedStamp[current.x, current.y] = pathStamp;
					foreach(var delta in dirs8)
					{
						var next = current + delta;
						if(!data.IsValidMapPosition(next))
							continue;
						if(closedStamp[next.x, next.y] == pathStamp)
							continue;

						float tentativeG = gScore[current.x, current.y] + CalculateStepCost(current, next);
						float nextG = gScoreStamp[next.x, next.y] == pathStamp ? gScore[next.x, next.y] : float.PositiveInfinity;
						if(tentativeG >= nextG)
							continue;

						cameFrom[next.x, next.y] = current;
						gScore[next.x, next.y] = tentativeG;
						gScoreStamp[next.x, next.y] = pathStamp;
						HeapPush((next, tentativeG + Heuristic(next, pb)));
					}
				}

				if(gScoreStamp[pb.x, pb.y] != pathStamp)
					return System.Array.Empty<Vector2Int>();

				var path = new List<Vector2Int>();
				var step = pb;
				while(step != pa)
				{
					path.Add(step);
					step = cameFrom[step.x, step.y];
				}
				path.Add(pa);
				path.Reverse();
				return path;
			}
			#endregion

			#region 锚点连边（K=3，确定性去重）
			const int nearestAnchorCount = 3;
			var anchorPairs = new List<(int a, int b)>();
			var pairSet = new HashSet<ulong>();
			for(int ai = 0; ai < anchors.Count; ++ai)
			{
				var nearest = new List<(int index, int sqrDistance)>();
				Vector2Int anchor = anchors[ai];

				for(int bi = 0; bi < anchors.Count; ++bi)
				{
					if(ai == bi)
						continue;

					int sqrDistance = (anchors[bi] - anchor).sqrMagnitude;
					nearest.Add((bi, sqrDistance));
				}

				nearest.Sort((lhs, rhs) =>
				{
					int distanceCompare = lhs.sqrDistance.CompareTo(rhs.sqrDistance);
					return distanceCompare != 0 ? distanceCompare : lhs.index.CompareTo(rhs.index);
				});

				int count = Mathf.Min(nearestAnchorCount, nearest.Count);
				for(int ni = 0; ni < count; ++ni)
				{
					int a = ai;
					int b = nearest[ni].index;
					if(a > b)
						(a, b) = (b, a);

					ulong pairKey = ((ulong)(uint)a << 32) | (uint)b;
					if(!pairSet.Add(pairKey))
						continue;

					anchorPairs.Add((a, b));
				}
			}
			#endregion

			#region 磨损迭代

			for(int i = 0; i < wearnessSampleCount; ++i)
			{
				for(int pi = 0; pi < anchorPairs.Count; ++pi)
				{
					var pair = anchorPairs[pi];
					Vector2Int pa = anchors[pair.a], pb = anchors[pair.b];
					AccumulatePath(FindPath(pa, pb).ToArray());
				}
				for(int y = 0; y < height; ++y)
				{
					for(int x = 0; x < width; ++x)
						tensors[x, y] *= wearnessDecayPerRound;
				}
			}
			#endregion
			// Optional: 扩散磨损张量数组，先不做

			/* 编码、输出结果 */

			Color[,] result = new Color[width, height];
			for(int y = 0; y < height; ++y)
			{
				for(int x = 0; x < width; ++x)
					result[x, y] = EncodeWearnessMapColor(tensors[x, y]);
			}
			return result;

			/* 辅助函数 */

			float CalculateStepCost(Vector2Int pa, Vector2Int pb)
			{
				// 允许 8 邻域：直走 1，对角 sqrt(2)
				float baseStep = Vector2Int.Distance(pa, pb);

				// ----------------------------
				// 1) 建筑距离软惩罚：有上限，不发散
				// ----------------------------
				// d=0 表示建筑占地（或紧贴建筑）。这里让它最多乘到 (1 + kBuilding)。
				// 建议 kBuilding 在 0.5~3 之间调，别太大，否则还是会压过一切。
				const float kBuilding = 1.5f;   // 建筑贴近惩罚强度（上限）
				const float falloff = 1.0f;     // 衰减尺度：越大，惩罚扩散得越远
				const float eps = 1e-4f;

				float da = distances[pa.x, pa.y];
				float db = distances[pb.x, pb.y];

				// 0..1：越靠近建筑越接近 1，距离越大越接近 0
				float wa = 1f / (1f + da / (falloff + eps));
				float wb = 1f / (1f + db / (falloff + eps));

				// 取平均（更平滑、不会因为一步踩到边缘就爆炸）
				float wBuilding = 0.5f * (wa + wb);

				// 乘子：1..(1+kBuilding)
				float buildingFactor = 1f + kBuilding * wBuilding;

				// ----------------------------
				// 2) 旧路奖励：用强度平均，不用乘积；显式力度
				// ----------------------------
				// strength 是 trace = xx + yy（你 CPU 那边已修正）。
				// 这里用均值更容易触发“黏路”，否则乘积会让奖励几乎消失。
				float sa = GetStrength(tensors[pa.x, pa.y]);
				float sb = GetStrength(tensors[pb.x, pb.y]);
				float s = 0.5f * (sa + sb);

				// 把 s 压到 0..1 的感觉（避免大 s 直接把路奖励饱和）
				// sScale 越大，越容易“形成路”；建议 0.2~2 之间试。
				const float sScale = 0.6f;
				float s01 = s / (s + sScale);

				// 路奖励力度：越大越黏路。建议 0.5~4 之间试。
				const float kRoad = 2.0f;

				// 乘子：从 1/(1+kRoad) 到 1 之间（有路更便宜，但不会无限便宜）
				float roadFactor = 1f / (1f + kRoad * s01);

				// ----------------------------
				// 3) 合成 + 全局尺度 + 最小步进代价
				// ----------------------------
				float cost = baseStep * buildingFactor * roadFactor * wearnessPenaltyScale;

				// 保底：确保 heuristic 下界依然是 1（配合你 A* 的启发式）
				cost = Mathf.Max(1f, cost);
				return cost;
			}

			void AccumulatePath(IList<Vector2Int> path)
			{
				if(path.Count <= 1)
					return;

				for(int i = 0; i < path.Count; ++i)
				{
					Vector2Int p = path[i];
					Vector2 movement = i == 0 ? path[i + 1] - p : p - path[i - 1];
					tensors[p.x, p.y] += CalculateWearnessTensor(movement);
				}
			}
		}

		/* 静态辅助函数 */

		static void InitializeBuildingDistanceField(int width, int height, int[,] distances, PolisData data)
		{
			var occupied = new bool[width, height];
			var queue = new Queue<Vector2Int>();

			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
					distances[x, y] = int.MaxValue;
			}

			// 多源 BFS：建筑占地为 0，向外按四邻域扩张得到曼哈顿距离。
			foreach(var building in data.buildings)
			{
				foreach(var p in data.YieldBuildingOccupancy(building))
				{
					if(!data.IsValidMapPosition(p) || occupied[p.x, p.y])
						continue;
					occupied[p.x, p.y] = true;
					distances[p.x, p.y] = 0;
					queue.Enqueue(p);
				}
			}

			if(queue.Count == 0)
			{
				int fallback = Mathf.Max(width, height);
				for(int x = 0; x < width; ++x)
				{
					for(int y = 0; y < height; ++y)
						distances[x, y] = fallback;
				}
				return;
			}

			var dirs4 = new Vector2Int[] {
				new(1, 0),
				new(-1, 0),
				new(0, 1),
				new(0, -1),
			};

			while(queue.Count > 0)
			{
				var current = queue.Dequeue();
				int nextDistance = distances[current.x, current.y] + 1;

				foreach(var delta in dirs4)
				{
					var next = current + delta;
					if(!data.IsValidMapPosition(next))
						continue;
					if(nextDistance >= distances[next.x, next.y])
						continue;

					distances[next.x, next.y] = nextDistance;
					queue.Enqueue(next);
				}
			}
		}

		static IEnumerable<Vector2Int> GetBuildingAnchors(PolisData data)
		{
			// 与 PolisData 的占地旋转规则一致。
			static Vector2Int Rot90(Vector2Int v, int quarterTurns)
			{
				quarterTurns = ((quarterTurns % 4) + 4) % 4;
				return quarterTurns switch
				{
					0 => v,
					1 => new Vector2Int(v.y, -v.x),
					2 => new Vector2Int(-v.x, -v.y),
					3 => new Vector2Int(-v.y, v.x),
					_ => v
				};
			}

			var anchors = new List<Vector2Int>();
			var anchorSet = new HashSet<Vector2Int>();
			foreach(var placement in data.buildings)
			{
				var definition = placement.Definition;
				if(definition == null)
					continue;

				int centerX = (definition.size.x - 1) / 2;
				var doorLocal = new Vector2Int(centerX - definition.pivot.x, definition.size.y - definition.pivot.y);
				var doorWorld = placement.position + Rot90(doorLocal, placement.orientation);
				if(!data.IsValidMapPosition(doorWorld) || anchorSet.Contains(doorWorld))
					continue;

				anchorSet.Add(doorWorld);
				anchors.Add(doorWorld);
			}

			return anchors;
		}

		static WearnessTensor CalculateWearnessTensor(Vector2 movement)
		{
			float theta = Mathf.Atan2(movement.y, movement.x) ;
			float cos = Mathf.Cos(theta), sin = Mathf.Sin(theta);
			return new(cos * cos, cos * sin, sin * sin);
		}

		static float GetStrength(in WearnessTensor tensor) => tensor.x + tensor.z;
		static float GetTheta(in WearnessTensor tensor) => 0.5f * Mathf.Atan2(2 * tensor.y, tensor.x - tensor.z);

		static Color EncodeWearnessMapColor(in WearnessTensor tensor)
		{
			float theta = GetTheta(tensor);
			float strength = GetStrength(tensor);
			float A = new Vector2(tensor.x - tensor.z, 2 * tensor.y).magnitude / (strength + 0.01f);
			Vector2 direction = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * A;
			return new(direction.x, direction.y, 0, strength);
		}
		#endregion
	}
}
