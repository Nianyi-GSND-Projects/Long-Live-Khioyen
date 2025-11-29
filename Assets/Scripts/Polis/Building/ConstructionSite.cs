using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class ConstructionSite : MonoBehaviour, IBuildingLike
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		static Material constructionMaterial;

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
		}

		public void OnDeselect()
		{
		}

		public void OnSelect()
		{
		}
	}
}
