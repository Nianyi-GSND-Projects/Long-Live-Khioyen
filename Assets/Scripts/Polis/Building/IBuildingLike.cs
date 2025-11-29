using UnityEngine;

namespace LongLiveKhioyen
{
	public interface IBuildingLike : ISelectable, IInspectable
	{
		public BuildingPlacement Placement { get; set; }
		public BuildingDefinition Definition { get; set; }
	}
}
