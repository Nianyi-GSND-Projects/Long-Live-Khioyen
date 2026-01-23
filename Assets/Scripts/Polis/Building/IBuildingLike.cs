using UnityEngine;

namespace LongLiveKhioyen
{
	public interface IBuildingLike : ISelectable
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }
	}
}
