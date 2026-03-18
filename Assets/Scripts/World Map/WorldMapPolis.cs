using UnityEngine;

namespace LongLiveKhioyen
{
	public class WorldMapPolis : MonoBehaviour
	{
		[SerializeField] SpriteRenderer image;

		public void Initialize(PolisData polisData)
		{
			name = polisData.id;
			image.sprite = polisData.Sprite;
			Debug.Log($"{name}: {image.sprite}", polisData.Sprite);
		}
	}
}
