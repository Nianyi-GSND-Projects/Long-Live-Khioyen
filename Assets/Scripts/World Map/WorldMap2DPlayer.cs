using UnityEngine;

namespace LongLiveKhioyen
{
	public class WorldMap2DPlayer : AbstractCharacterController
	{
		Rigidbody2D rb;
		float worldScale;
		protected void Awake()
		{
			rb = GetComponent<Rigidbody2D>();
			worldScale = GameInstance.Instance.Data.world.data2D.scale;
		}

		protected void Start()
		{
			prevPos = transform.localPosition;
		}

		public System.Action<Vector3> onMove;

		Vector3 prevPos;  // 场景暨世界空间
		public Vector2 WorldPos => prevPos;
		protected void Update()
		{
			Vector2 input = new(LateralMoveInput, ForwardMoveInput);

			// 理论上在地图上应该走的速度，未经 2D scale 缩放。
			Vector2 velocity = input * moveSpeed;
			rb.velocity = velocity * worldScale;
		}

		protected void FixedUpdate()
		{
			Vector2 movement = (transform.localPosition - prevPos) / worldScale;
			onMove?.Invoke(movement);

			prevPos = transform.localPosition;
		}

		public void Teleport(Vector2 worldPos)
		{
			transform.localPosition = prevPos = worldPos;
		}

		WorldMapPolis nearbyPolis;

		protected void OnTriggerEnter2D(Collider2D other)
		{
			if(!other.TryGetComponent<WorldMapPolis>(out var polis))
				return;
			nearbyPolis = polis;
		}

		protected void OnTriggerExit2D(Collider2D other)
		{
			if(!other.TryGetComponent<WorldMapPolis>(out var polis))
				return;
			if(polis == nearbyPolis)
				nearbyPolis = null;
		}

		public void InteractWithNearbyPolis()
		{
			if(nearbyPolis == null)
				return;
			var polis = nearbyPolis.PolisData;
			GameInstance.Instance.EnterPolis(polis.id);
		}
	}
}
