using UnityEngine;
using UnityEngine.UI;
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
			PolisData.Current.onProductionStateChanged += Refresh;
		}

		protected void OnDestroy()
		{
			PolisData.Current.onProductionStateChanged -= Refresh;
		}

		protected void Update()
		{
			// 更新制造进度条。
			float progress = 0f;
			var production = PolisData.Current.ProductionTask;
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
				var item = FancyListItem.Instantiate(recipesLayoutGroup.transform);
				item.ApplyItem(itemDefinition);
				item.Interactable = PolisData.Current.Economy.CanCover(itemDefinition.costs);
				item.onClick = () => PolisData.Current.QueueProduction(itemDefinition.itemId);
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
			if(!PolisData.Current.IsProducingItem)
				ProducingItem = null;
			else
			{
				var itemId = PolisData.Current.ProductionTask.parameters[0];
				ProducingItem = ItemDatabase.Instance.GetItem(itemId);
			}
		}

		public void RefreshQueued()
		{
			queuedLayoutGroup.transform.ClearChildren();

			foreach(var itemId in PolisData.Current.queuedProductions)
			{
				var itemDefinition = ItemDatabase.Instance.GetItem(itemId);

				var item = FancyListItem.Instantiate(queuedLayoutGroup.transform);
				item.ApplyItem(itemDefinition);
				item.SetCosts();
				item.Interactable = false;
			}

			queuedLayoutGroup.CalculateLayoutInputVertical();
		}
		#endregion
	}
}
