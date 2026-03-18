using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	[RequireComponent(typeof(AbstractCharacterController))]
	public class WorldMapPlayerArmy : MonoBehaviour
	{
		AbstractCharacterController controller;
		public AbstractCharacterController Controller
		{
			get
			{
				if(controller == null)
					controller = GetComponent<AbstractCharacterController>();
				return controller;
			}
		}

		PolisMiniature focusedMiniature;

		protected void OnTriggerEnter(Collider other)
		{
			if(!other.TryGetComponent<PolisMiniature>(out var miniature))
				return;
			focusedMiniature = miniature;
		}

		protected void OnTriggerExit(Collider other)
		{
			if(!other.TryGetComponent<PolisMiniature>(out var miniature))
				return;
			if(miniature == focusedMiniature)
				focusedMiniature = null;
		}

		public void InteractWithNearbyPolis()
		{
			if(focusedMiniature == null)
				return;
			var polis = focusedMiniature.data;
			GameInstance.Instance.EnterPolis(polis.id);
		}

		Vector3 lastPos;
		public System.Action<float> onMove;

		protected void Start()
		{
			lastPos = transform.position;
		}

		protected void Update()
		{
			Vector3 pos = transform.position, dPos = pos - lastPos;
			lastPos = pos;
			if(dPos != Vector3.zero)
			{
				float realDistance = dPos.magnitude / GameInstance.Instance.Data.world.data3D.scale;
				onMove?.Invoke(realDistance);
			}
		}
	}
}
