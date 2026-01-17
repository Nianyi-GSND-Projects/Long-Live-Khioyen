using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace LongLiveKhioyen
{
	[RequireComponent(typeof(NavMeshAgent))]
	public class PolisNpcController : AbstractCharacterController
	{
		NavMeshAgent agent;

		void Awake()
		{
			agent = GetComponent<NavMeshAgent>();
		}

		IEnumerator Start()
		{
			agent.speed = moveSpeed;
			agent.angularSpeed = lateralSpeed;

			yield return new WaitForEndOfFrame();  // 等待 NavMesh 构建完毕
			for(; ; )
			{
				Vector3 destination = Utilities.GetRandomPositionOnNavMesh(Polis.Instance.navMeshSurface);
				yield return NavigateToCoroutine(destination);
			}
		}

		void FixedUpdate()
		{
			ForwardMoveInput = agent.transform.worldToLocalMatrix.MultiplyVector(agent.velocity).z;
		}

		IEnumerator NavigateToCoroutine(Vector3 destination)
		{
			agent.destination = destination;
			yield return new WaitWhile(() => agent.pathPending);
			//destination = agent.pathEndPosition;
			yield return new WaitUntil(() => agent.remainingDistance < agent.radius);
		}
	}
}
