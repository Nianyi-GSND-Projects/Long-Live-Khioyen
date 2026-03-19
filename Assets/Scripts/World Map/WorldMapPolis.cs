using UnityEngine;
using TMPro;

namespace LongLiveKhioyen
{
	public class WorldMapPolis : MonoBehaviour
	{
		[SerializeField] SpriteRenderer image;
		[SerializeField] TMP_Text nameText;

		PolisData polisData;
		public PolisData PolisData
		{
			get => polisData;
			set
			{
				polisData = value;
				name = polisData.id;
				nameText.text = polisData.LocalizedName;
				image.sprite = polisData.Sprite;
			}
		}
	}
}
