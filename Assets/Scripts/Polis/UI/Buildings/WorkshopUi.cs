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
			Polis.Instance.onProductionStateChanged += Refresh;
		}

		protected void OnDestroy()
		{
			if(Polis.Instance)
				Polis.Instance.onProductionStateChanged -= Refresh;
		}

		protected void Update()
		{
			// 更新制造进度条。
			float progress = 0f;
			var production = Polis.Instance.Data.ProductionTask;
			if(production != null)
			{
				float totalTime = float.Parse(production.parameters[1]);
				progress = 1 - production.remainingTime / totalTime;
			}
			productionTimeBar.size = progress;
		}

		void Refresh()
		{
			RefreshRecipes();
			RefreshProducingRecipe();
			RefreshQueued();
		}

		#region 配方
		public class Recipe
		{
			public ItemDefinition item;
			public CostDescriptor[] costs;
		}

		void ApplyRecipeToItem(Recipe recipe, FancyListItem item)
		{
			item.ItemName = recipe.item.name;
			item.SetCosts(recipe.costs);
		}

		bool CheckRecipe(Recipe recipe)
		{
			var polis = Polis.Instance;
			foreach(var cost in recipe.costs)
			{
				switch(cost.type)
				{
					case EconomyType.Food:
						if(polis.Economy.food < cost.value)
							return false;
						break;
					case EconomyType.Material:
						if(polis.Economy.material < cost.value)
							return false;
						break;
					case EconomyType.Money:
						if(polis.Economy.money < cost.value)
							return false;
						break;
					case EconomyType.Population:
						if(polis.FreePopulation < cost.value)
							return false;
						break;
				}
			}
			return true;
		}
		#endregion

		#region 左侧配方列表
		public void RefreshRecipes()
		{
			recipesLayoutGroup.transform.ClearChildren();

			foreach(var recipe in GetRecipes())
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(recipesLayoutGroup.transform, false);
				ApplyRecipeToItem(recipe, item);
				item.Interactable = CheckRecipe(recipe);
				item.onClick = () => Polis.Instance.QueueProduction(recipe.item.itemId);
			}

			recipesLayoutGroup.CalculateLayoutInputVertical();
		}

		Recipe[] GetRecipes()
		{
			return ItemDatabase.Instance.items
				.Where(item => item.productable)
				.Select(item => new Recipe()
					{
						item = item,
						costs = item.costs,
					}
				)
				.ToArray();
		}
		#endregion

		#region 右侧制作中 & 制作队列
		public Recipe ProducingRecipe
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
					ApplyRecipeToItem(value, producingItem);
					producingNoItemSign.SetActive(false);
				}
			}
		}

		public void RefreshProducingRecipe()
		{
			if(!Polis.Instance.Data.IsProducingItem)
			{
				ProducingRecipe = null;
			}
			else
			{
				var itemId = Polis.Instance.Data.ProductionTask.parameters[0];
				var item = ItemDatabase.Instance.GetItem(itemId);
				ProducingRecipe = new() {
					item = item,
					costs = new CostDescriptor[0],
				};
			}
		}

		public void RefreshQueued()
		{
			queuedLayoutGroup.transform.ClearChildren();

			// TODO: DEBUG

			queuedLayoutGroup.CalculateLayoutInputVertical();
		}
		#endregion
	}
}
