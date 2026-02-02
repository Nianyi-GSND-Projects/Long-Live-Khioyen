using UnityEngine;
using Cinemachine;

namespace LongLiveKhioyen
{
	public partial class Polis
	{
		public enum Mode { Mayor, Wander }
		Mode currentMode = Mode.Mayor;
		public Mode CurrentMode => currentMode;

		public void SwitchMode()
		{
			switch(currentMode)
			{
				case Mode.Mayor:
					SwitchToMode(Mode.Wander);
					break;
				case Mode.Wander:
					SwitchToMode(Mode.Mayor);
					break;
			}
		}

		public void SwitchToMode(Mode mode)
		{
			if(mode != Mode.Mayor)
				SetMayorMode(false);
			if(mode != Mode.Wander)
				SetWanderMode(false);

			if(mode == Mode.Mayor)
				SetMayorMode(true);
			if(mode == Mode.Wander)
				SetWanderMode(true);

			currentMode = mode;
		}

		[Header("Control Mode")]
		public Transform anchor;
		public Vector3 AnchorPosition
		{
			get => anchor.position;
			set => anchor.position = ClampToMap(value);
		}
		public Vector3 AnchorEulers
		{
			get => anchor.eulerAngles;
			set => anchor.eulerAngles = value;
		}
		public float MayorDistance
		{
			get => -mayorCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset.z;
			set
			{
				var composer = mayorCamera.GetCinemachineComponent<CinemachineTransposer>();
				var offset = composer.m_FollowOffset;
				offset.z = -value;
				composer.m_FollowOffset = offset;
			}
		}

		public ConstructModal constructModal;
		public bool IsInConstructModal
		{
			get => constructModal.gameObject.activeInHierarchy;
			set
			{
				if(CurrentMode != Mode.Mayor)
					value = false;
				constructModal.gameObject.SetActive(value);
			}
		}

		#region Mayor mode
		[SerializeField] CinemachineVirtualCamera mayorCamera;

		void SetMayorMode(bool enabled)
		{
			mayorCamera.enabled = enabled;
			if(enabled)
				AnchorPosition = Vector3.ProjectOnPlane(player.transform.position, Vector3.up);
		}
		#endregion

		#region Wander mode
		[SerializeField] AbstractCharacterController player;
		[SerializeField] CinemachineVirtualCamera wanderCamera;

		void SetWanderMode(bool enabled)
		{
			wanderCamera.enabled = enabled;
			player.gameObject.SetActive(enabled);
			player.enabled = enabled;
			if(enabled)
			{
				AnchorPosition = ClosestWalkablePosition(AnchorPosition);
				player.Teleport(AnchorPosition);
				player.FaceTowards(Camera.main.transform.forward);
			}
		}
		#endregion
	}
}
