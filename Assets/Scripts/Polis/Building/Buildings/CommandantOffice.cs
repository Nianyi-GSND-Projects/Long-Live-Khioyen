using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 都尉府
	/// </summary>
	public class CommandantOffice : Building
	{
		const string uiAssetName = "Commandant Office";

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
