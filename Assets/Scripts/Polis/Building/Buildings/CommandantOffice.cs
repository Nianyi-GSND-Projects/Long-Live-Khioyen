using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	/// <summary>
	/// 都尉府
	/// </summary>
	public class CommandantOffice : Building
	{
		public override IEnumerable<InspectionAction> GetInspectionAction()
		{
			yield return new()
			{
				text = "Open",
				action = () => print("Player opens commandant office UI"),  // TODO
			};
		}

		const string inspectionUiTemplatePath = "Prefabs/Polis/UI/Inspection/Commandant Office";
		public override GameObject GetInspectionUi()
		{
			return Instantiate(Resources.Load<GameObject>(inspectionUiTemplatePath));
		}
	}
}
