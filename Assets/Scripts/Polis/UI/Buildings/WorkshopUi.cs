using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class WorkshopUi : MonoBehaviour
	{
		[SerializeField] LayoutGroup recipesLayoutGroup;
		[SerializeField] FancyListItem producingItem;
		[SerializeField] GameObject producingNoItemSign;
		[SerializeField] Scrollbar productionTimeBar;
		[SerializeField] LayoutGroup queuedLayoutGroup;

		protected void Start()
		{
			Refresh();
			Polis.Instance.Data.onProductionStateChanged += Refresh;
		}

		protected void OnDestroy()
		{
			Polis.Instance.Data.onProductionStateChanged -= Refresh;
		}

		protected void Update()
		{
			// 更新制造进度条。
			float progress = 0f;
			var production = Polis.Instance.Data.ProductionTask;
			if(production != null)
				progress = 1 - production.remainingTime / production.totalTime;
			productionTimeBar.size = progress;
		}

		void Refresh()
		{
			RefreshRecipes();
			RefreshProducingRecipe();
			RefreshQueued();
		}

		#region 左侧配方列表
		public void RefreshRecipes()
		{
			recipesLayoutGroup.transform.ClearChildren();

			foreach(var itemDefinition in ItemDatabase.Instance.items.Where(item => item.productable))
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(recipesLayoutGroup.transform, false);
				item.ItemName = itemDefinition.name;
				item.SetCosts(itemDefinition.costs);
				item.Interactable = Polis.Instance.Data.ValidateRecipeCost(itemDefinition.costs);
				item.onClick = () => Polis.Instance.Data.QueueProduction(itemDefinition.itemId);
			}

			recipesLayoutGroup.CalculateLayoutInputVertical();
		}
		#endregion

		#region 右侧制作中 & 制作队列
		public ItemDefinition ProducingItem
		{
			set
			{
				if(value == null)
				{
					producingItem.gameObject.SetActive(false);
					producingNoItemSign.SetActive(true);
				}
				else
				{
					producingItem.gameObject.SetActive(true);
					producingItem.ItemName = value.name;
					producingItem.SetCosts();
					producingNoItemSign.SetActive(false);
				}
			}
		}

		public void RefreshProducingRecipe()
		{
			if(!Polis.Instance.Data.IsProducingItem)
				ProducingItem = null;
			else
			{
				var itemId = Polis.Instance.Data.ProductionTask.parameters[0];
				ProducingItem = ItemDatabase.Instance.GetItem(itemId);
			}
		}

		public void RefreshQueued()
		{
			queuedLayoutGroup.transform.ClearChildren();

			foreach(var itemId in Polis.Instance.Data.queuedProductions)
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(queuedLayoutGroup.transform, false);
				var itemDefinition = ItemDatabase.Instance.GetItem(itemId);
				item.ItemName = itemDefinition.name;
				item.SetCosts();
				item.Interactable = false;
			}

			queuedLayoutGroup.CalculateLayoutInputVertical();
		}
		#endregion
	}
}
