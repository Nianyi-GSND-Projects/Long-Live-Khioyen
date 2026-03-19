using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
	public class WorldMapPolis : MonoBehaviour
	{
		[SerializeField] SpriteRenderer image;
		[SerializeField] TMP_Text nameText;

		public void Initialize(PolisData polisData)
		{
			name = polisData.id;
			nameText.text = polisData.LocalizedName;
			image.sprite = polisData.Sprite;
		}
	}
}
