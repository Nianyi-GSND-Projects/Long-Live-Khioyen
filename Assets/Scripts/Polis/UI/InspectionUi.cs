using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace LongLiveKhioyen
{
	public class InspectionUi : MonoBehaviour
	{
		[SerializeField] CanvasGroup canvasGroup;
		[SerializeField] TMP_Text title;
		[SerializeField] TMP_Text detail;

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

				UpdateContent();
			}
		}

		void UpdateContent()
		{
			if(building != null)
			{
				title.text = $"{buildingName}{(building.Placement.underConstruction ? " (constructing)" : string.Empty)}";
				detail.text = "TODO: Detail to be filled in here.";
			}
			else
			{
				title.text = string.Empty;
				detail.text = "Select a building to inspect.";
			}
		}

		void OnLocalizedBuildingNameChanged(string name)
		{
			buildingName = name;
			UpdateContent();
		}
	}
}
