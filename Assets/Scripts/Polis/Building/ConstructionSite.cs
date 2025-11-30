using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace LongLiveKhioyen
{
	public class ConstructionSite : MonoBehaviour, IBuildingLike
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		static Material constructionMaterial;

		#region Unity life cycle
		protected void Start()
		{
			if(constructionMaterial == null)
				constructionMaterial = Resources.Load<Material>("Materials/Polis/Construction_site");

			foreach(var renderer in GetComponentsInChildren<Renderer>())
			{
				var mArr = renderer.sharedMaterials;
				for(int i = 0; i < mArr.Length; ++i)
					mArr[i] = constructionMaterial;
				renderer.sharedMaterials = mArr;
			}

			var modifier = gameObject.AddComponent<NavMeshModifier>();
			modifier.applyToChildren = true;
			modifier.overrideArea = true;
			modifier.area = NavMesh.GetAreaFromName("Not Walkable");
		}
		#endregion

		#region Selection
		public void OnDeselect()
		{
		}

		public void OnSelect()
		{
		}

		public GameObject MakeUi()
		{
			return InspectionUi.CreateInstance(this).gameObject;
		}
		#endregion
	}
}
