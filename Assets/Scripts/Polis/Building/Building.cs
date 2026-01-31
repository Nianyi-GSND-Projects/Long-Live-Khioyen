using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class Building : MonoBehaviour, IBuildingLike
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		public const string inspectionUiPathPrefix = "Prefabs/Polis/UI/Inspection/";
		public const string buildingUiPathPrefix = "Prefabs/Polis/UI/Building Panels/";

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
			// TODO: 高亮显示
		}

		public void OnDeselect()
		{
		}

		public virtual IEnumerable<InspectionAction> GetInspectionAction()
		{
			yield break;
		}

		public virtual GameObject CreateInspectionUi()
		{
			return null;
		}

		/// <summary>
		/// 从 Resources 的固定路径里取得检视 UI。
		/// </summary>
		/// <param name="name">Prefab 文件名</param>
		protected GameObject GetInspectionUiByName(string name)
		{
			return HierarchyUtility.InstantiatePrefabFromResource(inspectionUiPathPrefix + name);
		}

		/// <summary>
		/// 打开建筑功能 UI。
		/// </summary>
		/// <param name="name">Prefab 文件名</param>
		protected void OpenBuildingUiByName(string name)
		{
			string uiTemplatePath = buildingUiPathPrefix + name;
			var ui = HierarchyUtility.InstantiatePrefabFromResource(uiTemplatePath);
			UiManager.Instance.OpenUiModal(ui, true);
		}
		#endregion
	}
}
