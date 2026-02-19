using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class HorseStationUi : MonoBehaviour
	{
		protected void Start()
		{
			PolisData.Current.StockedItems.onChanged += RefreshStocks;
			PolisData.Current.forSaleItems.onChanged += RefreshSell;

			IsSellOpen = true;

			Refresh();
		}

		protected void OnDestroy()
		{
			if(Polis.Instance)
			{
				PolisData.Current.StockedItems.onChanged -= RefreshStocks;
				PolisData.Current.forSaleItems.onChanged -= RefreshSell;
			}
		}

		public void Refresh()
		{
			RefreshStocks();
			RefreshSell();
			RefreshBuy();
		}

		#region 视觉状态
		public void SwitchSellBuy()
		{
			IsSellOpen ^= true;
		}

		public bool IsSellOpen
		{
			get => sellArea.activeSelf;
			set
			{
				sellArea.SetActive(value);
				buyArea.SetActive(!value);
			}
		}
		#endregion

		#region 库存
		[Header("Stocks")]
		[SerializeField] LayoutGroup stockedGoodsLayoutGroup;

		void RefreshStocks()
		{
			stockedGoodsLayoutGroup.transform.ClearChildren();

			var records = PolisData.Current.StockedItems;
			foreach(var itemDefinition in records.Definitions.Where(d => d.canSell))
			{
				var item = FancyListItem.Instantiate(stockedGoodsLayoutGroup.transform);
				item.ApplyItem(itemDefinition);
				item.ShowQuantity = true;
				item.Quantity = records.GetRecord(itemDefinition).quantity;
				item.SetCosts(
					new ResourceDescriptor() {
						type = ResourceType.Money,
						quantity = itemDefinition.sellPrice,
					}
				);
				item.onClick += () => PolisData.Current.SetItemForSale(itemDefinition.itemId, 1);
			}
		}
		#endregion

		#region 卖出
		[Header("Sell")]
		[SerializeField] GameObject sellArea;
		[SerializeField] LayoutGroup sellGoodsLayoutGroup;

		void RefreshSell()
		{
			sellGoodsLayoutGroup.transform.ClearChildren();

			var records = PolisData.Current.forSaleItems;
			foreach(var itemDefinition in records.Definitions)
			{
				var item = FancyListItem.Instantiate(sellGoodsLayoutGroup.transform);
				item.ApplyItem(itemDefinition);
				item.ShowQuantity = true;
				item.Quantity = records.GetRecord(itemDefinition).quantity;
				item.SetCosts(
					new ResourceDescriptor()
					{
						type = ResourceType.Money,
						quantity = itemDefinition.sellPrice,
					}
				);
				item.onClick += () => PolisData.Current.UnsetItemForSale(itemDefinition.itemId, 1);
			}
		}
		#endregion

		#region 买入
		[Header("Buy")]
		[SerializeField] GameObject buyArea;
		[SerializeField] LayoutGroup buyGoodsLayoutGroup;
		[SerializeField] LayoutGroup cartGoodsLayoutGroup;

		void RefreshBuy()
		{
			buyGoodsLayoutGroup.transform.ClearChildren();
			cartGoodsLayoutGroup.transform.ClearChildren();
		}
		#endregion
	}
}
