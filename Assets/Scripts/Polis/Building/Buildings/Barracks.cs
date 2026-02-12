using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class Barracks : Building
	{
		const string uiAssetName = "Barracks";

		public override IEnumerable<InspectionAction> GetInspectionAction()
		{
			yield return new()
			{
				text = "Open",
				action = () => OpenBuildingUiByName(uiAssetName),
			};
		}
	}
}
