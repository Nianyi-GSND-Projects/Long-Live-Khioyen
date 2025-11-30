using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
	public class InspectionUi : MonoBehaviour
	{
		[SerializeField] TMP_Text title;
		[SerializeField] TMP_Text detail;

		public string Title
		{
			set => title.text = value;
		}

		public string Detail
		{
			set => detail.text = value;
		}

		public System.Action onUpdate;

		protected void Update()
		{
			onUpdate?.Invoke();
		}
	}
}
