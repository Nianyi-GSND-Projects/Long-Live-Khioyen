using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class ExpeditionPanel : MonoBehaviour
	{
		#region 生命周期
		protected void Start()
		{
			Refresh();

			PolisData.Current.onGarrisonChanged += RefreshCommanders;
		}

		protected void OnDestroy()
		{
			PolisData.Current.onGarrisonChanged -= RefreshCommanders;
		}

		void Refresh()
		{
			RefreshCommanders();
		}
		#endregion

		#region 指挥官列表
		[SerializeField] LayoutGroup commandersLayoutGroup;

		void RefreshCommanders()
		{
			commandersLayoutGroup.transform.ClearChildren();
			var currentCommanders = PolisData.Current.GetGarrisonedCommanders();
			foreach(var commander in currentCommanders)
			{
				FancyListItem item = FancyListItem.Instantiate(commandersLayoutGroup.transform);
				item.ApplyCommander(commander);
				// TODO: item.onClick += 
			}
		}
		#endregion
	}
}
