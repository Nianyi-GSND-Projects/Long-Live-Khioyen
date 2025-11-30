using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

			Vector3 size = new(Definition.size.x, 0, Definition.size.y);
			Vector3 center = new Vector3(Definition.center.x, 0, Definition.center.y) - size * .5f;
			size.y = 1;

			// For building selection.
			var collider = gameObject.AddComponent<BoxCollider>();
			collider.isTrigger = Definition.obstructive;
			collider.size = size;
			collider.center = center;

			if(Definition.obstructive)
			{
				var obstale = gameObject.AddComponent<NavMeshObstacle>();
				obstale.carving = true;
				obstale.size = size;
				obstale.center = center;
			}
		}
		#endregion

		#region Selection
		public void OnSelect()
		{
		}

		public void OnDeselect()
		{
		}

		public GameObject MakeUi()
		{
			return InspectionUi.CreateInstance(this).gameObject;
		}
		#endregion
	}
}
