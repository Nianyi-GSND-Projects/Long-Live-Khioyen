using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class InspectionUi : MonoBehaviour
	{
		[SerializeField] CanvasGroup canvasGroup;
		[SerializeField] TMP_Text title;
		[SerializeField] TMP_Text detail;
		[SerializeField] LayoutGroup buttonLayoutGroup;

		const string buttonPrefabPath = "Prefabs/Polis/UI/Inspection Button";

		LocalizedString localizedBuildingName;
		string buildingName;

		IBuildingLike building;
		public IBuildingLike Building
		{
			get => building;
			set
			{
				if(building != null)
				{
					if(localizedBuildingName != null)
						localizedBuildingName.StringChanged -= OnLocalizedBuildingNameChanged;
				}

				building = value;

				if(building != null)
				{
					localizedBuildingName = building.Definition.GetLocalizedName();
					localizedBuildingName.StringChanged += OnLocalizedBuildingNameChanged;
				}

				SetupContent();
			}
		}

		void SetupContent()
		{
			title.text = string.Empty;
			detail.text = string.Empty;
			buttonLayoutGroup.transform.ClearChildren();

			if(building == null)
				return;

			title.text = $"{buildingName}{(building.Placement.underConstruction ? " (constructing)" : string.Empty)}";
			detail.text = "TODO: Detail to be filled in here.";

			AddButton("Inspect", () => print("Inspect"));
			buttonLayoutGroup.CalculateLayoutInputVertical();
		}

		void OnLocalizedBuildingNameChanged(string name)
		{
			buildingName = name;
			SetupContent();
		}

		void AddButton(string text, System.Action action)
		{
			var go = Instantiate(Resources.Load<GameObject>(buttonPrefabPath), buttonLayoutGroup.transform);

			go.GetComponentInChildren<TMP_Text>().text = text;

			var button = go.GetComponent<Button>();
			if(action != null)
				button.onClick.AddListener(() => action?.Invoke());
		}
	}
}
