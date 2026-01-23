using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace LongLiveKhioyen
{
	public class Building : MonoBehaviour, IBuildingLike
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		#region Life cycle
		protected void Start()
		{
			name = Definition.id;

			var modifier = gameObject.AddComponent<NavMeshModifier>();
			modifier.applyToChildren = true;
			modifier.overrideArea = true;
			modifier.area = NavMesh.GetAreaFromName("Not Walkable");
		}
		#endregion

		#region Selection
		public void OnSelect()
		{
		}

		public void OnDeselect()
		{
		}

		public virtual GameObject MakeUi()
		{
			// TODO: 每个功能建筑能构造独特的 UI。
			return null;
		}
		#endregion
	}
}
