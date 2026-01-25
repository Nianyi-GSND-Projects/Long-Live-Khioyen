using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class Workshop : Building
	{
		const string uiAssetName = "Workshop";

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
