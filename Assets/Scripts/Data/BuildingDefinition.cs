using UnityEngine;
using UnityEngine.Localization;

namespace LongLiveKhioyen
{
	[CreateAssetMenu(menuName = "Long Live Khioyen/Building Definition")]
	public class BuildingDefinition : ScriptableObject
	{
		public string id;
		public string[] tags;
		public Sprite figure;

		[Header("Geometry")]
		[Range(0, 3)] public int defaultOrientation;
		public Vector2Int pivot;
		public Vector2Int size;
		public Vector2 center;

		public GameObject ModelTemplate => Resources.Load<GameObject>($"Models/Buildings/{id}");

		[Header("Construction")]
		[Min(0)] public float constructionTime;
		public int requiredPopulation;
		public Economy cost;

		public LocalizedString GetLocalizedName()
		{
			return new("Building Names", id);
		}
	}
}
