using UnityEngine;
using UnityEngine.Localization;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
	[CreateAssetMenu(menuName = "Long Live Khioyen/Building Definition")]
	public class BuildingDefinition : ScriptableObject
	{
		public string id;
		public string[] tags;
		public Sprite figure;

		[Header("几何")]
		[Range(0, 3)] public int defaultOrientation;
		public Vector2Int pivot;
		public Vector2Int size;
		public Vector2 center;

		public GameObject ModelTemplate => Resources.Load<GameObject>($"Models/Buildings/{id}");

		[Header("条件")]
		[Label("是否能建造"), Tooltip("即是否是只能在初始存档里配的特殊建筑")] public bool canConstruct = true;
		[Label("前置建筑 tag 列表"), Tooltip("此列表中的任意 tag 在当前已建成建筑中没有则无法修建")] public string[] preliminaryBuildingTags = new string[0];

		[Header("花费")]
		[Label("建造时间（月）")] public float constructionTime;
		/// <summary>修建时所需要的人口占用。</summary>
		[Label("建造占用人口"), Tooltip("建造完成后归还此部分占用人口"), Min(0)] public int constructionPopulation;
		/// <summary>建好后的人口占用。</summary>
		[Label("周转所需人口"), Tooltip("建造完成后才生效"), Min(0)] public int persistentPopulation;
		[Label("建造消耗资源")] public Economy cost;

		public LocalizedString GetLocalizedName()
		{
			return new("Building Names", id);
		}
	}
}
