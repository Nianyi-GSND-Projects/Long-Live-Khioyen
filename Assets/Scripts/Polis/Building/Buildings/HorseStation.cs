using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class HorseStation : Building
	{
		const string uiAssetName = "Horse Station";

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
