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

		void Start()
		{
			agent.speed = moveSpeed;
			agent.angularSpeed = lateralSpeed;
			StartCoroutine(nameof(WanderCoroutine));
		}

		void FixedUpdate()
		{
			ForwardMoveInput = agent.transform.worldToLocalMatrix.MultiplyVector(agent.velocity).z;
		}

		IEnumerator WanderCoroutine()
		{
			for(; ; )
			{
				Vector3 destination = Utilities.GetRandomPositionOnHavMesh(Polis.Instance.navMeshSurface);
				yield return NavigateToCoroutine(destination);
			}
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
