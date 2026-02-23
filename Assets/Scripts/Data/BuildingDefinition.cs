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
		/// <summary>建好后的人口占用。</summary>
		public int persistentPopulation;

		[Header("Geometry")]
		[Range(0, 3)] public int defaultOrientation;
		public Vector2Int pivot;
		public Vector2Int size;
		public Vector2 center;

		public GameObject ModelTemplate => Resources.Load<GameObject>($"Models/Buildings/{id}");

		[Header("Construction")]
		[Min(0)] public float constructionTime;
		/// <summary>修建时所需要的人口占用。</summary>
		[UnityEngine.Serialization.FormerlySerializedAs("requiredPopulation")] public int constructionPopulation;
		public Economy cost;

		public LocalizedString GetLocalizedName()
		{
			return new("Building Names", id);
		}
	}
}
