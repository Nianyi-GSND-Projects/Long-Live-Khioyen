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

			foreach(var meshCollider in GetComponentsInChildren<MeshCollider>())
				Destroy(meshCollider);

			Vector3 size = new(Definition.size.x, 0, Definition.size.y);
			Vector3 center = new Vector3(Definition.center.x, 0, Definition.center.y) - size * .5f;
			size.y = 1;

			var collider = gameObject.AddComponent<BoxCollider>();
			collider.size = size;
			collider.center = center;

			var obstale = gameObject.AddComponent<NavMeshObstacle>();
			obstale.carving = true;
			obstale.size = size;
			obstale.center = center;
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
