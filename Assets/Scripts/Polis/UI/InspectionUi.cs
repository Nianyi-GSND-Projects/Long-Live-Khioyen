using UnityEngine;
using UnityEngine.Localization;
using TMPro;

namespace LongLiveKhioyen
{
	public class InspectionUi : MonoBehaviour
	{
		[SerializeField] TMP_Text title;
		[SerializeField] TMP_Text detail;

		static GameObject template;
		public static InspectionUi CreateInstance(IBuildingLike building)
		{
			if(template == null)
				template = Resources.Load<GameObject>("Prefabs/Polis/UI/Inspection UI");
			var instance = Instantiate(template).GetComponent<InspectionUi>();
			instance.Building = building;
			return instance;
		}

		public IBuildingLike Building { get; set; }
		LocalizedString localizedBuildingName;
		string buildingName;

		protected void Start()
		{
			localizedBuildingName = Building.Definition.GetLocalizedName();
			localizedBuildingName.StringChanged += name =>
			{
				buildingName = name;
				UpdateContent();
			};
		}

		void UpdateContent()
		{
			title.text = $"{buildingName}{(Building.Placement.underConstruction ? " (constructing)" : string.Empty)}";
			detail.text = "";
		}
	}
}
