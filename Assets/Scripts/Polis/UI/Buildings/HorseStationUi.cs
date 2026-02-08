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
			Polis.Instance.Data.stockedItems.onChanged += RefreshStocks;
			Polis.Instance.Data.forSaleItems.onChanged += RefreshSell;

			IsSellOpen = true;

			Refresh();
		}

		protected void OnDestroy()
		{
			if(Polis.Instance)
			{
				Polis.Instance.Data.stockedItems.onChanged -= RefreshStocks;
				Polis.Instance.Data.forSaleItems.onChanged -= RefreshSell;
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

			var records = Polis.Instance.Data.stockedItems;
			foreach(var itemDefinition in records.Definitions.Where(d => d.canSell))
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(stockedGoodsLayoutGroup.transform, false);

				item.ItemName = itemDefinition.name;
				item.ShowQuantity = true;
				item.Quantity = records.GetRecord(itemDefinition).quantity;
				item.SetCosts(
					new CostDescriptor() {
						type = EconomyType.Money,
						value = itemDefinition.sellPrice,
					}
				);
				item.onClick += () => Polis.Instance.Data.SetItemForSale(itemDefinition.itemId, 1);
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

			var records = Polis.Instance.Data.forSaleItems;
			foreach(var itemDefinition in records.Definitions)
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(sellGoodsLayoutGroup.transform, false);

				item.ItemName = itemDefinition.name;
				item.ShowQuantity = true;
				item.Quantity = records.GetRecord(itemDefinition).quantity;
				item.SetCosts(
					new CostDescriptor()
					{
						type = EconomyType.Money,
						value = itemDefinition.sellPrice,
					}
				);
				item.onClick += () => Polis.Instance.Data.UnsetItemForSale(itemDefinition.itemId, 1);
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
