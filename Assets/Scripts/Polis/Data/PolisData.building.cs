using UnityEngine;
using System;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public partial class PolisData
	{
		public List<BuildingPlacement> buildings;
	}

	[Serializable]
	public class BuildingPlacement
	{
		public string id;  // The building ID stored in the definition sheet.
		public Vector2Int position;
		[Range(0, 3)] public int orientation;  // By 90 degrees.

		public bool underConstruction;
	}
}
