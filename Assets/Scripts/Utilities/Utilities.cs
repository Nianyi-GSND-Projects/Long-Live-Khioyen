using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nianyi.UnityPack;
using System.Collections.Generic;

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

		/// <summary>
		/// 仅根据 PolisData 中的建筑布局，计算磨损方向图。
		/// 返回值每个格点是“方向*强度”向量，范围约在 [-1, 1]。
		/// </summary>
		public static Vector2[,] CalculateWearnessVectors(
			PolisData data,
			int fixedSamples,
			float trafficDiscountPerPass = 0.12f,
			float nearBuildingPenalty = 0.8f,
			float minStepCost = 0.15f,
			float decayPerRound = 0.95f
		)
		{
			int width = data.size.x;
			int height = data.size.y;

			var blocked = new bool[width, height];
			var nearBuilding = new bool[width, height];
			var flow = new Vector2[width, height];
			var heat = new float[width, height];
			var passCount = new int[width, height];

			// 1) 建筑默认挡路：把所有建筑占据格标记为不可通行。
			foreach(var placement in data.buildings)
			{
				foreach(var p in data.YieldBuildingOccupancy(placement))
				{
					if(p.x >= 0 && p.x < width && p.y >= 0 && p.y < height)
						blocked[p.x, p.y] = true;
				}
			}

			// 与 PolisData 里的朝向旋转规则保持一致。
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

			bool IsWalkable(Vector2Int p)
			{
				return p.x >= 0 && p.x < width
					&& p.y >= 0 && p.y < height
					&& !blocked[p.x, p.y];
			}

			var dirs8 = new Vector2Int[] {
				new(1, 0),
				new(-1, 0),
				new(0, 1),
				new(0, -1),
				new(1, 1),
				new(1, -1),
				new(-1, 1),
				new(-1, -1),
			};

			// 2) 预计算“建筑周围一圈”的惩罚区。
			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
				{
					if(!blocked[x, y])
						continue;

					var c = new Vector2Int(x, y);
					foreach(var d in dirs8)
					{
						var n = c + d;
						if(n.x < 0 || n.x >= width || n.y < 0 || n.y >= height)
							continue;
						if(!blocked[n.x, n.y])
							nearBuilding[n.x, n.y] = true;
					}
				}
			}

			Vector2Int FindNearestWalkable(Vector2Int seed)
			{
				if(IsWalkable(seed))
					return seed;

				var visited = new bool[width, height];
				var queue = new Queue<Vector2Int>();
				if(seed.x >= 0 && seed.x < width && seed.y >= 0 && seed.y < height)
				{
					queue.Enqueue(seed);
					visited[seed.x, seed.y] = true;
				}
				else
				{
					seed = new Vector2Int(Mathf.Clamp(seed.x, 0, width - 1), Mathf.Clamp(seed.y, 0, height - 1));
					queue.Enqueue(seed);
					visited[seed.x, seed.y] = true;
				}

				while(queue.Count > 0)
				{
					var current = queue.Dequeue();
					if(IsWalkable(current))
						return current;

					foreach(var d in dirs8)
					{
						var next = current + d;
						if(next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
							continue;
						if(visited[next.x, next.y])
							continue;
						visited[next.x, next.y] = true;
						queue.Enqueue(next);
					}
				}

				return new Vector2Int(-1, -1);
			}

			List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
			{
				if(!IsWalkable(start) || !IsWalkable(goal))
					return null;
				if(start == goal)
					return new List<Vector2Int> { start };

				bool CanStep(Vector2Int from, Vector2Int to)
				{
					if(!IsWalkable(to))
						return false;

					int dx = to.x - from.x;
					int dy = to.y - from.y;
					// 对角移动时避免“穿角”。
					if(dx != 0 && dy != 0)
					{
						var sideA = new Vector2Int(from.x + dx, from.y);
						var sideB = new Vector2Int(from.x, from.y + dy);
						if(!IsWalkable(sideA) || !IsWalkable(sideB))
							return false;
					}

					return true;
				}

				float StepCost(Vector2Int from, Vector2Int to)
				{
					int dx = Mathf.Abs(to.x - from.x);
					int dy = Mathf.Abs(to.y - from.y);
					float baseCost = (dx != 0 && dy != 0) ? 1.41421356f : 1f;
					float penalty = nearBuilding[to.x, to.y] ? nearBuildingPenalty : 0f;
					float discount = passCount[to.x, to.y] * trafficDiscountPerPass;

					// 确保每步代价始终大于 0，避免异常路径偏好。
					return Mathf.Max(minStepCost, baseCost + penalty - discount);
				}

				float Heuristic(Vector2Int a, Vector2Int b)
				{
					// Octile 距离，适配八邻域。
					int dx = Mathf.Abs(a.x - b.x);
					int dy = Mathf.Abs(a.y - b.y);
					int min = Mathf.Min(dx, dy);
					int max = Mathf.Max(dx, dy);
					return (max - min) + 1.41421356f * min;
				}

				var closed = new bool[width, height];
				var inOpen = new bool[width, height];
				var cameFrom = new Vector2Int[width, height];
				var gScore = new float[width, height];
				var fScore = new float[width, height];
				for(int x = 0; x < width; ++x)
					for(int y = 0; y < height; ++y)
					{
						cameFrom[x, y] = new Vector2Int(-1, -1);
						gScore[x, y] = float.PositiveInfinity;
						fScore[x, y] = float.PositiveInfinity;
					}

				var open = new List<Vector2Int>();
				open.Add(start);
				inOpen[start.x, start.y] = true;
				gScore[start.x, start.y] = 0f;
				fScore[start.x, start.y] = Heuristic(start, goal);

				while(open.Count > 0)
				{
					int bestIndex = 0;
					float bestF = fScore[open[0].x, open[0].y];
					for(int i = 1; i < open.Count; ++i)
					{
						var p = open[i];
						float f = fScore[p.x, p.y];
						if(f < bestF)
						{
							bestF = f;
							bestIndex = i;
						}
					}

					var current = open[bestIndex];
					open.RemoveAt(bestIndex);
					inOpen[current.x, current.y] = false;
					if(current == goal)
						break;
					closed[current.x, current.y] = true;

					foreach(var d in dirs8)
					{
						var next = current + d;
						if(!CanStep(current, next))
							continue;
						if(closed[next.x, next.y])
							continue;

						float tentativeG = gScore[current.x, current.y] + StepCost(current, next);
						if(tentativeG >= gScore[next.x, next.y])
							continue;

						cameFrom[next.x, next.y] = current;
						gScore[next.x, next.y] = tentativeG;
						fScore[next.x, next.y] = tentativeG + Heuristic(next, goal);

						if(!inOpen[next.x, next.y])
						{
							open.Add(next);
							inOpen[next.x, next.y] = true;
						}
					}
				}

				if(cameFrom[goal.x, goal.y].x < 0)
					return null;

				var path = new List<Vector2Int>();
				var step = goal;
				while(step != start)
				{
					path.Add(step);
					step = cameFrom[step.x, step.y];
				}
				path.Add(start);
				path.Reverse();
				return path;
			}

			void AccumulatePath(List<Vector2Int> path, float weight)
			{
				if(path == null || path.Count < 2)
					return;

				// 先记录“通行次数”，让后续采样更倾向复用已有路径。
				for(int i = 0; i < path.Count; ++i)
				{
					var p = path[i];
					passCount[p.x, p.y] += 1;
				}

				for(int i = 0; i < path.Count - 1; ++i)
				{
					var a = path[i];
					var b = path[i + 1];
					Vector2 dir = ((Vector2)(b - a)).normalized * weight;
					// 把方向统一到同一半球，避免 A->B 与 B->A 相互抵消。
					if(dir.y < 0f || (Mathf.Abs(dir.y) <= 1e-6f && dir.x < 0f))
						dir = -dir;

					flow[a.x, a.y] += dir;
					flow[b.x, b.y] += dir * 0.5f;
					heat[a.x, a.y] += weight;
					heat[b.x, b.y] += weight * 0.5f;
				}
			}

			void DecayFlow(float decay)
			{
				for(int x = 0; x < width; ++x)
				{
					for(int y = 0; y < height; ++y)
					{
						flow[x, y] *= decay;
						heat[x, y] *= decay;
					}
				}
			}

			// 2) 生成采样锚点：建筑门口（默认定义）+ 市中心。
			var anchors = new List<Vector2Int>();
			foreach(var placement in data.buildings)
			{
				var def = placement.Definition;
				if(def == null)
					continue;

				int centerX = (def.size.x - 1) / 2;
				var doorLocal = new Vector2Int(centerX - def.pivot.x, def.size.y - def.pivot.y);
				var doorWorld = placement.position + Rot90(doorLocal, placement.orientation);
				var walkableDoor = FindNearestWalkable(doorWorld);
				if(IsWalkable(walkableDoor))
					anchors.Add(walkableDoor);
			}

			var center = FindNearestWalkable(new Vector2Int(width / 2, height / 2));
			if(IsWalkable(center))
				anchors.Add(center);

			for(int i = 0; i < fixedSamples; ++i)
			{
				for(int si = 0; si < anchors.Count; ++si)
				{
					for(int ti = si + 1; ti < anchors.Count; ++ti)
					{
						var start = anchors[si];
						var target = anchors[ti];
						var path = FindPath(start, target);
						AccumulatePath(path, 1f);
					}
				}
				DecayFlow(decayPerRound);
			}

			float maxHeat = 0.0001f;
			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
				{
					if(heat[x, y] > maxHeat)
						maxHeat = heat[x, y];
				}
			}

			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
				{
					float strength = Mathf.Clamp01(heat[x, y] / maxHeat);
					Vector2 dir = flow[x, y];
					if(dir.sqrMagnitude > 1e-8f)
						flow[x, y] = dir.normalized * strength;
					else
						flow[x, y] = Vector2.zero;
				}
			}

			return flow;
		}
	}
}
