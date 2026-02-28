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
		public static Vector2[,] CalculateWearnessVectors(PolisData data, int fixedSamples = 192)
		{
			int width = data.size.x;
			int height = data.size.y;

			var blocked = new bool[width, height];
			var flow = new Vector2[width, height];

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

				var visited = new bool[width, height];
				var cameFrom = new Vector2Int[width, height];
				for(int x = 0; x < width; ++x)
					for(int y = 0; y < height; ++y)
						cameFrom[x, y] = new Vector2Int(-1, -1);

				var queue = new Queue<Vector2Int>();

				queue.Enqueue(start);
				visited[start.x, start.y] = true;

				while(queue.Count > 0)
				{
					var current = queue.Dequeue();
					if(current == goal)
						break;

					foreach(var d in dirs8)
					{
						var next = current + d;
						if(!CanStep(current, next))
							continue;
						if(visited[next.x, next.y])
							continue;

						visited[next.x, next.y] = true;
						cameFrom[next.x, next.y] = current;
						queue.Enqueue(next);
					}
				}

				if(!visited[goal.x, goal.y])
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

				for(int i = 0; i < path.Count - 1; ++i)
				{
					var a = path[i];
					var b = path[i + 1];
					Vector2 dir = ((Vector2)(b - a)).normalized * weight;

					flow[a.x, a.y] += dir;
					flow[b.x, b.y] += dir * 0.5f;
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

			if(anchors.Count >= 2)
			{
				int seed = (width * 73856093) ^ (height * 19349663) ^ (data.buildings.Count * 83492791);
				var rng = new System.Random(seed);

				for(int i = 0; i < fixedSamples; ++i)
				{
					int si = rng.Next(anchors.Count);
					int ti = rng.Next(anchors.Count - 1);
					if(ti >= si)
						ti += 1;

					var start = anchors[si];
					var target = anchors[ti];
					var path = FindPath(start, target);
					AccumulatePath(path, 1f);
				}
			}

			// 3) 归一化并做轻度增强，得到可直接给 Shader 的方向场。
			float maxMagnitude = 0.0001f;
			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
				{
					float m = flow[x, y].magnitude;
					if(m > maxMagnitude)
						maxMagnitude = m;
				}
			}

			for(int x = 0; x < width; ++x)
			{
				for(int y = 0; y < height; ++y)
				{
					Vector2 normalized = flow[x, y] / maxMagnitude;
					float m = normalized.magnitude;
					float boosted = Mathf.Clamp01(Mathf.Pow(m, 0.75f) * 1.8f);
					flow[x, y] = m > 0.0001f ? normalized.normalized * boosted : Vector2.zero;
				}
			}

			return flow;
		}
	}
}
