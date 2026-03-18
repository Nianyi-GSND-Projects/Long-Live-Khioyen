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
			float dt = Time.deltaTime;
			Vector3 input = new(LateralMoveInput, ForwardMoveInput, 0);
			// 理论上在地图上应该走的距离，未经 2D scale 缩放。
			Vector3 movement = input * (moveSpeed * dt);

			if(movement.sqrMagnitude > 0)
			{
				onMove?.Invoke(movement);
				transform.localPosition += movement * GameInstance.Instance.Data.world.data2D.scale;
			}
		}
	}
}
