using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public interface IBuildingLike : ISelectable
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }

		public class InspectionAction
		{
			public string text;  // TODO: 支持本地化
			public System.Action action;
		}
		public IEnumerable<InspectionAction> GetInspectionAction();
		public GameObject GetInspectionUi();
	}
}
