using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
	public class PolisMiniature : MonoBehaviour
	{
		public PolisData data;
		[SerializeField] TMP_Text polisNameText;

		void Start()
		{
			name = data.id;
			polisNameText.text = data.id;
		}
	}
}
