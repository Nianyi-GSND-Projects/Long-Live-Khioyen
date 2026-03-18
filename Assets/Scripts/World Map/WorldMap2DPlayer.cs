using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class WorldMap2DPlayer : AbstractCharacterController
	{
		public System.Action<Vector3> onMove;

		protected void Update()
		{
			Vector3 input = new(LateralMoveInput, ForwardMoveInput, 0);
			Vector3 movement = input * (moveSpeed * GameInstance.Instance.Data.world.data2D.scale);
			if(movement.sqrMagnitude > 0)
			{
				onMove?.Invoke(movement);
				transform.localPosition += movement;
			}
		}
	}
}
