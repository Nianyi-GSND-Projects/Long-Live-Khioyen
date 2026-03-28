using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace LongLiveKhioyen
{
	public class HorseStationUi : MonoBehaviour
	{
		Dictionary<string, int> buyCart = new();
		GameObject cartItemPrefab;

		protected void Start()
		{
			PolisData.Current.StockedItems.onChanged += RefreshStocks;
			PolisData.Current.forSaleItems.onChanged += RefreshSell;
			PolisData.Current.MonthlyBuyItems.onChanged += RefreshBuy;

			cartItemPrefab = Resources.Load<GameObject>("Prefabs/Polis/UI/Cart Item");

			IsSellOpen = true;

			Refresh();
		}

		protected void OnDestroy()
		{
			if(Polis.Instance)
			{
				PolisData.Current.StockedItems.onChanged -= RefreshStocks;
				PolisData.Current.forSaleItems.onChanged -= RefreshSell;
				PolisData.Current.MonthlyBuyItems.onChanged -= RefreshBuy;
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

		public void SellAll()
		{
			var stocks = PolisData.Current.StockedItems.Where(r => r.Definition.canSell).ToArray();
			foreach(var s in stocks)
				PolisData.Current.SetItemForSale(s.itemId, s.quantity);
		}
		#endregion

		#region 买入
		[Header("Buy")]
		[SerializeField] GameObject buyArea;
		[SerializeField] LayoutGroup buyGoodsLayoutGroup;
		[SerializeField] LayoutGroup cartGoodsLayoutGroup;
		[SerializeField] TMP_Text totalPriceText;
		[SerializeField] Button buyButton;

		void RefreshBuy()
		{
			PolisData.Current.EnsureMonthlyBuyItems();
			RefreshBuyGoodsView();
			RefreshCartView();
		}

		void RefreshBuyGoodsView()
		{
			buyGoodsLayoutGroup.transform.ClearChildren();

			var records = PolisData.Current.MonthlyBuyItems;
			foreach(var itemDefinition in records.Definitions)
			{
				int stockCount = records.GetRecord(itemDefinition).quantity;
				int inCartCount = buyCart.TryGetValue(itemDefinition.itemId, out int current) ? current : 0;
				int availableCount = stockCount - inCartCount;
				if(availableCount <= 0)
					continue;

				var item = FancyListItem.Instantiate(buyGoodsLayoutGroup.transform);
				item.ApplyItem(itemDefinition);
				item.ShowQuantity = true;
				item.Quantity = availableCount;
				item.SetCosts(
					new ResourceDescriptor()
					{
						type = ResourceType.Money,
						quantity = itemDefinition.buyPrice,
					}
				);
				item.onClick += () => AddToCart(itemDefinition.itemId, 1);
			}
		}

		void AddToCart(string itemId, int quantity)
		{
			if(quantity <= 0)
				return;

			var monthlyItems = PolisData.Current.MonthlyBuyItems;
			int stockCount = monthlyItems.GetItemQuantity(itemId);
			int currentCount = buyCart.TryGetValue(itemId, out int current) ? current : 0;
			int canAdd = Mathf.Min(quantity, stockCount - currentCount);
			if(canAdd <= 0)
				return;

			buyCart[itemId] = currentCount + canAdd;
			RefreshBuyGoodsView();
			RefreshCartView();
		}

		void RemoveFromCart(string itemId, int quantity)
		{
			if(quantity <= 0)
				return;

			if(!buyCart.TryGetValue(itemId, out int currentCount))
				return;

			currentCount -= quantity;
			if(currentCount <= 0)
				buyCart.Remove(itemId);
			else
				buyCart[itemId] = currentCount;

			RefreshBuyGoodsView();
			RefreshCartView();
		}

		void RefreshCartView()
		{
			cartGoodsLayoutGroup.transform.ClearChildren();

			float totalPrice = 0;
			foreach(var pair in buyCart)
			{
				var item = ItemDatabase.Instance.GetItem(pair.Key);
				totalPrice += item.buyPrice * pair.Value;

				for(int i = 0; i < pair.Value; i++)
				{
					var cartItem = Instantiate(cartItemPrefab, cartGoodsLayoutGroup.transform);
					cartItem.GetComponent<Image>().sprite = item.icon;
					cartItem.GetComponent<Button>().onClick.AddListener(() => RemoveFromCart(item.itemId, 1));
				}
			}

			totalPriceText.text = totalPrice.ToString("0");
			buyButton.interactable = buyCart.Count > 0;
		}

		/// <summary>确认购物车并执行购买，供 Inspector 绑定按钮事件。</summary>
		public void ConfirmBuyCart()
		{
			if(buyCart.Count == 0)
				return;

			// 用快照执行购买，避免购买过程触发 UI 刷新时修改原字典导致枚举器失效。
			var purchaseList = buyCart.ToArray();

			float totalPrice = purchaseList
				.Select(pair => ItemDatabase.Instance.GetItem(pair.Key).buyPrice * pair.Value)
				.Sum();
			if(!PolisData.Current.Economy.CanCover(new ResourceDescriptor() { type = ResourceType.Money, quantity = totalPrice, }))
			{
				Debug.LogWarning("钱财不足，无法完成购买。");
				return;
			}

			foreach(var pair in purchaseList)
				PolisData.Current.BuyItem(pair.Key, pair.Value);

			buyCart.Clear();
			RefreshBuyGoodsView();
			RefreshCartView();
		}
		#endregion
	}
}
