using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
	public class InspectionUi : MonoBehaviour
	{
		[SerializeField] CanvasGroup canvasGroup;
		[SerializeField] TMP_Text title;
		[SerializeField] LayoutGroup detailArea;
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

		protected void OnDestroy()
		{
			if(building != null)
			{
				if(localizedBuildingName != null)
					localizedBuildingName.StringChanged -= OnLocalizedBuildingNameChanged;
			}
		}

		void SetupContent()
		{
			title.text = string.Empty;
			detailArea.transform.ClearChildren();
			buttonLayoutGroup.transform.ClearChildren();

			if(building == null)
				return;

			title.text = buildingName;
			var ui = building.CreateInspectionUi();
			if(ui != null)
				ui.transform.SetParent(detailArea.transform, false);
			detailArea.CalculateLayoutInputVertical();

			foreach(var action in building.GetInspectionAction())
			{
				if(action == null)
					continue;
				AddActionButton(action);
			}
			buttonLayoutGroup.CalculateLayoutInputVertical();
		}

		void OnLocalizedBuildingNameChanged(string name)
		{
			buildingName = name;
			SetupContent();
		}

		void AddActionButton(InspectionAction action)
		{
			var go = Instantiate(Resources.Load<GameObject>(buttonPrefabPath), buttonLayoutGroup.transform);

			go.GetComponentInChildren<TMP_Text>().text = action.text;

			var button = go.GetComponent<Button>();
			if(action.action != null)
				button.onClick.AddListener(() => action.action?.Invoke());
		}
	}
}
