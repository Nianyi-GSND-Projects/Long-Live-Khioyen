using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace LongLiveKhioyen
{
	public class MayorMode : MonoBehaviour
	{
		Polis Polis => Polis.Instance;

		#region Settings
		[Header("Settings")]
		public float panSpeed = 1;
		public float zoomSpeed = 1;
		public Vector2 zoomRange = new(2, 100);
		public float rotateSpeed = 1;
		public float minAzimuth = 10f;
		#endregion

		#region Input states
		Vector2 pointerScreenPosition;
		public Vector2 PointerScreenPosition => pointerScreenPosition;
		bool isPointerOverGameObjects;

		bool isPrimaryButtonDown, isSecondaryButtonDown;
		float lastPrimaryClickTime;
		Vector2 primaryStartScreenPosition;
		#endregion

		#region Life cycle
		void Update()
		{
			isPointerOverGameObjects = EventSystem.current?.IsPointerOverGameObject() ?? false;
		}
		#endregion

		#region Input handlers
		protected void OnPoint(InputValue value)
		{
			var raw = value.Get<Vector2>();
			pointerScreenPosition = raw;
		}

		protected void OnDrag(InputValue value)
		{
			var raw = value.Get<Vector2>();

			if(isPrimaryButtonDown)
				Pan(raw);
			if(isSecondaryButtonDown)
				Rotate(raw);
		}

		protected void OnScroll(InputValue value)
		{
			var raw = value.Get<float>();
			Zoom(raw);
		}

		protected void OnPrimaryClick(InputValue value)
		{
			if(isPointerOverGameObjects)
				return;

			var raw = value.isPressed;
			isPrimaryButtonDown = raw;

			if(raw)
			{
				lastPrimaryClickTime = Time.realtimeSinceStartup;
				primaryStartScreenPosition = pointerScreenPosition;
			}
			else
			{
				float elapsedTime = Time.realtimeSinceStartup - lastPrimaryClickTime;
				Vector2 mouseMoved = primaryStartScreenPosition - pointerScreenPosition;
				if(
					elapsedTime <= 0.3f &&
					mouseMoved.magnitude <= 5 &&
					!isPointerOverGameObjects
				)
					Interact(pointerScreenPosition);
			}
		}

		protected void OnSecondaryClick(InputValue value)
		{
			var raw = value.isPressed;
			isSecondaryButtonDown = raw;
		}
		#endregion

		#region Functions
		void Pan(Vector2 screenDelta)
		{
			if(!Polis.ScreenToGround(pointerScreenPosition - screenDelta, out var to))
				return;
			if(!Polis.ScreenToGround(pointerScreenPosition, out var from))
				return;
			Polis.AnchorPosition += (to - from) * panSpeed;
		}

		void Zoom(float scrollY)
		{
			float z = Polis.MayorDistance;
			z *= Mathf.Exp(-scrollY * zoomSpeed);
			z = Mathf.Clamp(z, zoomRange.x, zoomRange.y);
			Polis.MayorDistance = z;
		}

		void Rotate(Vector2 screenDelta)
		{
			screenDelta.y *= -1;
			screenDelta *= rotateSpeed;

			var euler = Polis.AnchorEulers;
			euler.x = Mathf.Clamp(euler.x + screenDelta.y, minAzimuth, 90);
			euler.y += screenDelta.x;

			Polis.AnchorEulers = euler;
		}

		protected void Interact(Vector2 screenPos)
		{
			if(Polis.IsInConstructModal)
			{
				Polis.constructModal.TryPlaceBuilding();
				return;
			}

			var ray = Camera.main.ScreenPointToRay(screenPos);
			if(!Physics.Raycast(ray, out var hit, Mathf.Infinity))
				return;

			Polis.Selection = hit.collider.GetComponentsInParent<ISelectable>();
		}
		#endregion
	}
}
