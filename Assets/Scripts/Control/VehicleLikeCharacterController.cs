using UnityEngine;

namespace LongLiveKhioyen
{
	[RequireComponent(typeof(CharacterController))]
	public class VehicleLikeCharacterController : AbstractCharacterController
	{
		CharacterController controller;

		void Awake()
		{
			controller = GetComponent<CharacterController>();
		}

		void Update()
		{
			float dt = Time.deltaTime;
			controller.SimpleMove(transform.forward * (ForwardMoveInput * moveSpeed));
			transform.Rotate(0, LateralMoveInput * lateralSpeed * dt, 0);
		}

		void LateUpdate()
		{
			transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
		}

		public override void Teleport(Vector3 position)
		{
			controller.enabled = false;
			base.Teleport(position);
			controller.enabled = true;
		}
	}
}
