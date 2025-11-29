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
			Polis.onEconomyChanged += OnEconomyDataChanged;

			localizedBuildingName = new("Building Names", "");
			localizedBuildingName.StringChanged += s => text.text = s;

			button.onClick.AddListener(() => onSelected?.Invoke(this));
		}

		protected void Start()
		{
			localizedBuildingName.TableEntryReference = buildingDefinition.id;
			localizedBuildingName.RefreshString();
			image.sprite = buildingDefinition.figure;
		}

		protected void OnDestroy()
		{
			if(Polis)
				Polis.onEconomyChanged -= OnEconomyDataChanged;
		}

		void OnEconomyDataChanged()
		{
			if(buildingDefinition.cost <= Polis.Economy)
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
