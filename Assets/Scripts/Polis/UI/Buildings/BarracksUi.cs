using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public class BarracksUi : MonoBehaviour
	{
		#region Life cycle
		protected void Start()
		{
			PolisData.Current.onGarrisonChanged += RefreshCommanders;
			PolisData.Current.onNextPromotableCommanderChanged += RefreshPromotion;

			Refresh();
			InspectCommander(null);
		}

		protected void OnDestroy()
		{
			PolisData.Current.onGarrisonChanged -= RefreshCommanders;
			PolisData.Current.onNextPromotableCommanderChanged -= RefreshPromotion;
		}

		void Refresh()
		{
			RefreshPromotion();
			RefreshCommanders();
		}
		#endregion

		#region 提拔
		[SerializeField] Button promotionButton;
		[SerializeField] LayoutGroup promotionSlot;  // 暂时不用，如果将来改成同时可提拔多个再开。
		[SerializeField] FancyListItem promotionItem;

		void RefreshPromotion()
		{
			GameCommander commander = PolisData.Current.GetPromotableCommander();
			promotionItem.ApplyCommander(commander);
			promotionItem.Interactable = false;
			promotionButton.interactable = commander != null;
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
				FancyListItem item = FancyListItem.Instantiate(commandersLayoutGroup.transform);
				item.ApplyCommander(commander);
				item.onClick += () => InspectCommander(commander);
			}
		}
		#endregion

		#region 右侧细节面板
		[Header("右侧细节")]
		[SerializeField] Image commanderAvatarImage;
		[SerializeField] TMP_Text commanderNameText;
		[SerializeField] TMP_Text zhiText, xinText, renText, yongText, yanText;

		void InspectCommander(GameCommander commander)
		{
			commanderAvatarImage.gameObject.SetActive(commander?.portrait != null);
			commanderAvatarImage.sprite = commander?.portrait;
			commanderNameText.text = commander?.commanderName ?? string.Empty;

			zhiText.text = commander?.Zhi.ToString();
			xinText.text = commander?.Xin.ToString();
			renText.text = commander?.Ren.ToString();
			yongText.text = commander?.Yong.ToString();
			yanText.text = commander?.Yan.ToString();
		}
		#endregion
	}
}
