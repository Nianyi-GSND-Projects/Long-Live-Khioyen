using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
	public class BarracksUi : MonoBehaviour
	{
		#region Life cycle
		protected void Start()
		{
			PolisData.Current.onGarrisonChanged += () => RefreshCommanders();
			PolisData.Current.onNextPromotableCommanderChanged += () => RefreshPromotion();

			Refresh();
		}

		protected void OnDestroy()
		{
			PolisData.Current.onGarrisonChanged -= () => RefreshCommanders();
			PolisData.Current.onNextPromotableCommanderChanged -= () => RefreshPromotion();
		}

		void Refresh()
		{
			RefreshPromotion();
			RefreshCommanders();
		}
		#endregion

		#region 辅助
		void ApplyCommanderToItem(GameCommander commander, FancyListItem item)
		{
			item.Interactable = commander != null;
			item.ItemName = commander?.commanderName ?? string.Empty;
			item.IconSprite = commander?.portrait;
			item.SetCosts();
		}
		#endregion

		#region 提拔
		[SerializeField] Button promotionButton;
		[SerializeField] LayoutGroup promotionSlot;  // 暂时不用，如果将来改成同时可提拔多个再开。
		[SerializeField] FancyListItem promotionItem;

		void RefreshPromotion()
		{
			GameCommander commander = PolisData.Current.GetPromotableCommander();
			ApplyCommanderToItem(commander, promotionItem);
		}

		public void Promote()
		{
			PolisData.Current.PromoteCommander();
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
				FancyListItem item = FancyListItem.Instantiate();
				item.transform.SetParent(commandersLayoutGroup.transform, false);
				ApplyCommanderToItem(commander, item);
				item.onClick += () => InspectCommander(commander);
			}
		}
		#endregion

		#region 右侧细节面板
		void InspectCommander(GameCommander commander)
		{
			//
		}
		#endregion
	}
}
