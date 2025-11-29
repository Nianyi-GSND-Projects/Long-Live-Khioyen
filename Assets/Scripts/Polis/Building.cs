using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class Building : MonoBehaviour, ISelectable, IInspectable
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		#region Life cycle
		protected void Start()
		{
			name = Definition.id;

			model = Instantiate(Definition.ModelTemplate);
			model.name = "Model";
			model.transform.SetParent(transform, false);
			foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
				legacyMaterials[renderer] = renderer.sharedMaterials;
			constructionMaterial = Resources.Load<Material>("Materials/Polis/Construction_site");

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

			UpdateVisualState();
		}
		#endregion

		#region Visual state
		GameObject model;
		readonly Dictionary<Renderer, Material[]> legacyMaterials = new();
		Material constructionMaterial;

		public void UpdateVisualState()
		{
			if(Placement.underConstruction)
			{
				foreach(var renderer in legacyMaterials.Keys)
					renderer.sharedMaterial = constructionMaterial;
			}
			else
			{
				foreach(var (renderer, mats) in legacyMaterials)
					renderer.sharedMaterials = mats;
			}
		}

		public bool UnderConstruction
		{
			get => Placement.underConstruction;
			set
			{
				Placement.underConstruction = value;
				UpdateVisualState();
			}
		}

		public void OnSelect()
		{
		}

		public void OnDeselect()
		{
		}
		#endregion
	}
}
