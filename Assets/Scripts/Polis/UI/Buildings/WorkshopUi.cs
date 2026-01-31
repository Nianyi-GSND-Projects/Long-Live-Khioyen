using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class WorkshopUi : MonoBehaviour
	{
		[SerializeField] LayoutGroup recipesLayoutGroup;
		[SerializeField] FancyListItem producingItem;
		[SerializeField] GameObject producingNoItemSign;
		[SerializeField] LayoutGroup queuedLayoutGroup;
		[SerializeField] FancyListItem completedItem;
		[SerializeField] GameObject completedPlaceholder;
		[SerializeField] Button collectButton;

		protected void Start()
		{
			RefreshRecipes();
			ProducingRecipe = null;  // TODO
			RefreshQueued();
			CompletedRecipe = null;  // TODO
		}

		public class Recipe
		{
			public string name;
			public FancyListItem.CostDescriptor[] costs;
		}

		void ApplyRecipeToItem(Recipe recipe, FancyListItem item)
		{
			item.ItemName = recipe.name;
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

		public void RefreshRecipes()
		{
			recipesLayoutGroup.transform.ClearChildren();

			foreach(var recipe in GetRecipes())
			{
				var item = FancyListItem.Instantiate();
				item.transform.SetParent(recipesLayoutGroup.transform, false);
				ApplyRecipeToItem(recipe, item);
				item.Interactable = CheckRecipe(recipe);
			}

			recipesLayoutGroup.CalculateLayoutInputVertical();
		}

		Recipe[] GetRecipes()
		{
			// TODO: DEBUG
			return new Recipe[]
			{
				new()
				{
					name = "青椒炒蛋",
					costs = new FancyListItem.CostDescriptor[]
					{
						new()
						{
							type = EconomyType.Food,
							value = 2,
						},
					},
				},
				new()
				{
					name = "宋顺文炸蛋",
					costs = new FancyListItem.CostDescriptor[]
					{
						new()
						{
							type = EconomyType.Food,
							value = 1,
						},
						new()
						{
							type = EconomyType.Population,
							value = 1,
						},
					},
				},
				new()
				{
					name = "鬼推磨",
					costs = new FancyListItem.CostDescriptor[]
					{
						new()
						{
							type = EconomyType.Population,
							value = -1,
						},
						new()
						{
							type = EconomyType.Material,
							value = 10,
						},
						new()
						{
							type = EconomyType.Money,
							value = 1000,
						},
					},
				},
			};
		}

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

		public void RefreshQueued()
		{
			queuedLayoutGroup.transform.ClearChildren();

			// TODO: DEBUG

			queuedLayoutGroup.CalculateLayoutInputVertical();
		}

		public Recipe CompletedRecipe
		{
			set
			{
				if(value == null)
				{
					completedItem.gameObject.SetActive(false);
					completedPlaceholder.SetActive(true);
					collectButton.interactable = false;
				}
				else
				{
					completedItem.gameObject.SetActive(true);
					ApplyRecipeToItem(value, completedItem);
					completedPlaceholder.SetActive(false);
					collectButton.interactable = true;
				}
			}
		}
	}
}
