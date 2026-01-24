using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;
using Nianyi.UnityPack;

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
	}
}
