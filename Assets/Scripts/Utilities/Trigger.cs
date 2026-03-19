using UnityEngine;
using UnityEngine.Events;

namespace LongLiveKhioyen
{
	public class Trigger : MonoBehaviour
	{
		#region Serialized fields
		public bool oneTime;
		public UnityEvent onEnter;
		public UnityEvent onExit;
		#endregion

		#region Life cycle
		protected void OnTriggerEnter(Collider _)
		{
			onEnter?.Invoke();
		}

		protected void OnTriggerEnter2D(Collider2D _)
		{
			onEnter?.Invoke();
		}

		protected void OnTriggerExit(Collider _)
		{
			onExit?.Invoke();
			if(oneTime)
				Destroy(this);
		}

		protected void OnTriggerExit2D(Collider2D _)
		{
			onExit?.Invoke();
			if(oneTime)
				Destroy(this);
		}
		#endregion
	}
}
