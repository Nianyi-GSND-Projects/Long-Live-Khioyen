using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using System;
using TMPro;

namespace LongLiveKhioyen
{
	public class ConstructOptionCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		Polis Polis => Polis.Instance;
		[NonSerialized] public BuildingDefinition buildingDefinition;
		LocalizedString localizedBuildingName;

		CanvasGroup group;
		[SerializeField] Button button;
		[SerializeField] TMP_Text text;
		[SerializeField] Image image;

		public Action<ConstructOptionCard> onSelected, onHovered, onUnhovered;

		protected void Awake()
		{
			group = GetComponent<CanvasGroup>();
			button.onClick.AddListener(() => onSelected?.Invoke(this));
		}

		protected void Start()
		{
			localizedBuildingName = buildingDefinition.GetLocalizedName();
			localizedBuildingName.StringChanged += s => text.text = s;
			image.sprite = buildingDefinition.figure;

			Polis.Data.onEconomyChanged += OnEconomyDataChanged;
		}

		protected void OnDestroy()
		{
			Polis.Data.onEconomyChanged -= OnEconomyDataChanged;
		}

		void OnEconomyDataChanged()
		{
			if(Polis.Data.Economy.CanCover(buildingDefinition.cost))
			{
				group.interactable = true;
				group.alpha = 1;
			}
			else
			{
				group.interactable = false;
				group.alpha = 0.5f;
			}
		}

		public void OnPointerEnter(PointerEventData eventData) => onHovered?.Invoke(this);

		public void OnPointerExit(PointerEventData eventData) => onUnhovered?.Invoke(this);
	}
}
